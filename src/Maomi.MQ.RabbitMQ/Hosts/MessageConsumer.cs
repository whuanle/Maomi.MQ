// <copyright file="MessageConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace Maomi.MQ.Hosts;

/// <summary>
/// The consumer server responsible for handling MQ messages.
/// </summary>
/// <typeparam name="TMessage">TMessage.</typeparam>
public class MessageConsumer<TMessage>
    where TMessage : class
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IConsumerOptions _consumerOptions;
    protected readonly Func<IServiceProvider, object> _consumerInstance;
    protected readonly ILogger _logger;
    protected readonly IConsumerDiagnostics _consumerDiagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConsumer{TMessage}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="consumerInstance"></param>
    public MessageConsumer(
        IServiceProvider serviceProvider,
        IConsumerOptions consumerOptions,
        Func<IServiceProvider, object> consumerInstance)
    {
        var serviceFactory = serviceProvider.GetRequiredService<ServiceFactory>();
        _serviceProvider = serviceProvider;
        _mqOptions = serviceFactory.Options;

        _policyFactory = serviceFactory.RetryPolicyFactory;
        _consumerDiagnostics = serviceFactory.ConsumerDiagnostics;

        _consumerOptions = consumerOptions;
        _consumerInstance = consumerInstance;
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MessageConsumer<TMessage>>();
    }

    public virtual async Task ConsumerAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
    {
        MessageHeader messageHeader = eventArgs.GetMessageHeader();
        using Activity? activity = _consumerDiagnostics.StartConsume(messageHeader, eventArgs, _consumerOptions);

        IConsumer<TMessage> consumer = default!;
        try
        {
            consumer = (_consumerInstance(_serviceProvider) as IConsumer<TMessage>)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An exception occurred when obtaining the consumer instance {0}.", $"IConsumer<{typeof(TMessage).Name}>");
        }

        if (consumer == null)
        {
            var breakdown = _serviceProvider.GetRequiredService<IBreakdown>();
            await breakdown.NotFoundConsumerAsync(_consumerOptions.Queue, typeof(TMessage), typeof(IConsumer<TMessage>));

            var ex = new ArgumentNullException(nameof(consumer), "The consumer instance cannot be null.");
            _consumerDiagnostics.ExceptionConsume(messageHeader, ex, activity);
            throw ex;
        }

        ConsumerState fallbackState = ConsumerState.Ack;
        TMessage? eventBody = null;

        try
        {
            IMessageSerializer? messageSerializer = default;
            foreach (var serializer in _mqOptions.MessageSerializers)
            {
                if (serializer.ContentType == messageHeader.ContentType)
                {
                    messageSerializer = serializer;
                    break;
                }
            }

            if (messageSerializer == null)
            {
                throw new InvalidOperationException($"No suitable message serializer was found for content type '{messageHeader.ContentType}'.");
            }

            eventBody = messageSerializer.Deserialize<TMessage>(eventArgs.Body.Span)!;
            if (eventBody == null)
            {
                ArgumentNullException.ThrowIfNull(eventBody, "The message body cannot be null.");
            }
        }
        catch (Exception ex)
        {
            _consumerDiagnostics.ExceptionConsume(messageHeader, ex, activity);
            fallbackState = await FallbackAsync(eventArgs, consumer, messageHeader, eventBody, ex);
            goto Fallback;
        }

        // Executed on the last retry.
        // 最后一次重试失败时执行.
        var fallbackPolicy = Policy<ConsumerState>
            .Handle<Exception>()
            .FallbackAsync(async (c) =>
            {
                return await FallbackAsync(eventArgs, consumer, messageHeader, eventBody, null, c);
            });

        int retryCount = 0;

        // Custom retry policy.
        // 自定义重试策略.
        AsyncRetryPolicy customRetryPolicy = await _policyFactory.CreatePolicy(_consumerOptions.Queue, messageHeader.Id);
        var policyWrap = fallbackPolicy.WrapAsync(customRetryPolicy);

        // 执行消费和重试.
        fallbackState = await policyWrap.ExecuteAsync(async () =>
        {
            Interlocked.Increment(ref retryCount);
            var executeState = await ExecuteAndRetryAsync(eventArgs, consumer, messageHeader, eventBody, retryCount);
            return executeState;
        });

    /*
     * ACK or NACK based on the result.
     * 根据结果进行 ACK 或 NACK.
     */

    Fallback:

        // The execution completed normally, or the FallbackAsync function was executed to compensate for the last retry.
        // 正常执行完成，或执行了 FallbackAsync 函数补偿最后一次重试.
        if (fallbackState == ConsumerState.Ack)
        {
            await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        }

        // Whether to put it back to the queue when the last retry fails.
        // 最后一次重试失败时，是否放回队列.
        else if (fallbackState == ConsumerState.Nack)
        {
            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.RetryFaildRequeue);
        }
        else if (fallbackState == ConsumerState.NackAndRequeue)
        {
            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
        else if (fallbackState == ConsumerState.NackAndNoRequeue)
        {
            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        else
        {
            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.RetryFaildRequeue);
        }

        _consumerDiagnostics.StopConsume(messageHeader, activity);
    }

    protected virtual async Task<ConsumerState> ExecuteAndRetryAsync(BasicDeliverEventArgs eventArgs, IConsumer<TMessage> consumer, MessageHeader messageHeader, TMessage eventBody, int retryCount)
    {
        using Activity? executeActivity = _consumerDiagnostics.StartExecute(messageHeader);

        try
        {
            await consumer.ExecuteAsync(messageHeader, eventBody);
            _consumerDiagnostics.StopExecute(messageHeader, executeActivity);
        }
        catch (Exception ex)
        {
            _consumerDiagnostics.RecordFail(messageHeader, _consumerOptions);
            _consumerDiagnostics.ExceptionExecute(messageHeader, ex, executeActivity);
            _consumerDiagnostics.StopExecute(messageHeader, executeActivity);

            using Activity? retryActivity = _consumerDiagnostics.StartRetry(messageHeader);

            // Each retry.
            // 每次失败时执行.
            try
            {
                await consumer.FaildAsync(messageHeader, ex, retryCount, eventBody);
                _consumerDiagnostics.StopRetry(messageHeader, retryActivity);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(
                    faildEx,
                    "An error occurred while executing the FaildAsync method,exchange [{Exchange}],routing [{RoutingKey}],id [{Id}],deliveryTag [{DeliveryTag}].",
                    eventArgs.Exchange,
                    eventArgs.RoutingKey,
                    messageHeader.Id,
                    eventArgs.DeliveryTag);

                _consumerDiagnostics.ExceptionRetry(messageHeader, faildEx, retryActivity);
                _consumerDiagnostics.StopRetry(messageHeader, retryActivity);

                throw;
            }

            throw;
        }

        return ConsumerState.Ack;
    }

    protected virtual async Task<ConsumerState> FallbackAsync(BasicDeliverEventArgs eventArgs, IConsumer<TMessage> consumer, MessageHeader messageHeader, TMessage? eventBody, Exception? ex,CancellationToken cancellationToken = default)
    {
        var fallbackActivity = _consumerDiagnostics.StartFallback(messageHeader);
        ConsumerState fallbackState = ConsumerState.Ack;

        try
        {
            var fallbackResult = await consumer.FallbackAsync(messageHeader, eventBody, ex);
            return fallbackResult;
        }
        catch (Exception fallbackEx)
        {
            _logger.LogWarning(
                fallbackEx,
                "An error occurred while executing the FallbackAsync method,exchange [{Exchange}],routing [{RoutingKey}],id [{Id}],deliveryTag [{DeliveryTag}].",
                eventArgs.Exchange,
                eventArgs.RoutingKey,
                messageHeader.Id,
                eventArgs.DeliveryTag);

            _consumerDiagnostics.ExceptionFallback(messageHeader, fallbackEx, fallbackActivity);
            return ConsumerState.Exception;
        }
        finally
        {
            _consumerDiagnostics.StopFallback(messageHeader, fallbackState, fallbackActivity);
        }
    }
}
