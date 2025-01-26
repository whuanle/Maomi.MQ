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
using System.Diagnostics.Metrics;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Consumer.
/// </summary>
public class MessageConsumer
{
    protected static readonly DiagnosticListener _diagnosticListener = new DiagnosticListener(DiagnosticName.Listener.Consumer);
    protected static readonly ActivitySource _activitySource = new ActivitySource(DiagnosticName.ActivitySource.Consumer);

    protected readonly Meter _meter;
    protected readonly Counter<int> _pullMessageCount;
    protected readonly Counter<int> _messageFaildCount;
    protected readonly Histogram<long> _messageSize;
    protected readonly TagList _tags;

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly IMessageSerializer _messageSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IConsumerOptions _consumerOptions;
    protected readonly Func<IServiceProvider, object> _consumerInstance;
    protected readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConsumer"/> class.
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

        _messageSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _consumerOptions = consumerOptions;
        _consumerInstance = consumerInstance;
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(DiagnosticName.Consumer);

        _tags = new TagList
        {
            { "AppId", _mqOptions.AppName },
            { "Queue", consumerOptions.Queue },
            { "Exchange", consumerOptions.BindExchange },
            { "RoutingKey", consumerOptions.RoutingKey },
        };

        var meterFactory = _serviceProvider.GetService<IMeterFactory>();
        _meter = meterFactory != null ? meterFactory.Create(DiagnosticName.Meter.Consumer) : SharedMeter.Consumer;

        _pullMessageCount = _meter.CreateCounter<int>("maomimq.consumer.message.pull.count", "{request}", "The number of messages pushed or pulled by the server", _tags);
        _messageFaildCount = _meter.CreateCounter<int>("maomimq.consumer.message.faild.count", "{request}", "The total number of retries for processing messages", _tags);
        _messageSize = _meter.CreateHistogram<long>("maomimq.consumer.message.received", "Byte", "The size of the received message", _tags);
    }

    public virtual async Task ConsumerAsync<TMessage>(IChannel channel, BasicDeliverEventArgs eventArgs)
        where TMessage : class
    {
        using Activity? activity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Consumer, ActivityKind.Consumer);

        MessageHeader messageHeader = eventArgs.BasicProperties.GetMessageHeader();

        OnStartEvent(messageHeader, eventArgs, _consumerOptions.Queue, activity);

        var consumer = _consumerInstance(_serviceProvider) as IConsumer<TMessage>;
        if (consumer == null)
        {
            var breakdown = _serviceProvider.GetRequiredService<IBreakdown>();
            await breakdown.NotFoundConsumer(_consumerOptions.Queue, typeof(TMessage), typeof(IConsumer<TMessage>));

            var ex = new ArgumentNullException(nameof(consumer), "The consumer instance cannot be null.");
            OnExceptionEvent(messageHeader, ex, activity);
            throw ex;
        }

        ConsumerState fallbackState = ConsumerState.Ack;
        TMessage? eventBody = null;

        try
        {
            eventBody = _messageSerializer.Deserialize<TMessage>(eventArgs.Body.Span)!;
            if (eventBody == null)
            {
                ArgumentNullException.ThrowIfNull(eventBody, "The message body cannot be null.");
            }
        }
        catch (Exception ex)
        {
            OnExceptionEvent(messageHeader, ex, activity);
            fallbackState = await FallbackAsync(eventArgs, consumer, messageHeader, eventBody, ex);
            goto Fallback;
        }

        // Executed on the last retry.
        // 最后一次重试失败时执行.
        var fallbackPolicy = Policy<ConsumerState>
            .Handle<Exception>()
            .FallbackAsync(async (c) =>
            {
                return await FallbackAsync(eventArgs, consumer, messageHeader, eventBody, null);
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

        OnEndEvent(messageHeader, activity);
    }

    protected virtual async Task<ConsumerState> ExecuteAndRetryAsync<TMessage>(BasicDeliverEventArgs eventArgs, IConsumer<TMessage> consumer, MessageHeader messageHeader, TMessage eventBody, int retryCount)
        where TMessage : class
    {
        using Activity? executekActivity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Execute, ActivityKind.Internal);

        try
        {
            executekActivity?.Start();
            await consumer.ExecuteAsync(messageHeader, eventBody);
            executekActivity?.Stop();
        }
        catch (Exception ex)
        {
            _messageFaildCount.Add(1);
            executekActivity?.AddException(ex);
            executekActivity?.Stop();

            using Activity? retrykActivity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Retry, ActivityKind.Internal);

            // Each retry.
            // 每次失败时执行.
            try
            {
                retrykActivity?.Start();
                await consumer.FaildAsync(messageHeader, ex, retryCount, eventBody);
                retrykActivity?.Stop();
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

                retrykActivity?.AddException(faildEx);
                retrykActivity?.Stop();

                throw;
            }

            throw;
        }

        return ConsumerState.Ack;
    }

    protected virtual async Task<ConsumerState> FallbackAsync<TMessage>(BasicDeliverEventArgs eventArgs, IConsumer<TMessage> consumer, MessageHeader messageHeader, TMessage? eventBody, Exception? ex)
        where TMessage : class
    {
        var fallbackActivity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Fallback, ActivityKind.Internal);
        OnFallbackStartEvent(messageHeader, eventArgs, _consumerOptions.Queue, fallbackActivity);
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

            OnFallbackExceptionEvent(messageHeader, fallbackEx, fallbackActivity);
            return ConsumerState.Exception;
        }
        finally
        {
            OnFallbackEndEvent(messageHeader, fallbackState, fallbackActivity);
        }
    }

    protected bool IsEnabledListener()
    {
        // check if there is a parent Activity or if someone listens to "<Maomi.MQ.Publisher>" ActivitySource or "MaomiMQPublisherHandlerDiagnosticListener" DiagnosticListener.
        return Activity.Current != null ||
               _activitySource.HasListeners() ||
               _diagnosticListener.IsEnabled();
    }

    protected virtual void OnStartEvent(MessageHeader messageHeader, BasicDeliverEventArgs eventArgs, string queue, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.AddTag("Id", messageHeader.Id);
        activity.AddTag("Timestamp", messageHeader.Timestamp);
        activity.AddTag("AppId", messageHeader.AppId);
        activity.AddTag("ContentEncoding", messageHeader.ContentEncoding);
        activity.AddTag("ContentType", messageHeader.ContentType);
        activity.AddTag("Type", messageHeader.Type);
        activity.AddTag("UserId", messageHeader.UserId);
        activity.AddTag("Exchange", eventArgs.Exchange);
        activity.AddTag("RoutingKey", eventArgs.RoutingKey);
        activity.AddTag("Queue", queue);

        activity.Start();

        _pullMessageCount.Add(1);
        _messageSize.Record(eventArgs.Body.Length);

        _diagnosticListener.Write(DiagnosticName.Event.ConsumerStart, messageHeader);
    }

    protected virtual void OnEndEvent(MessageHeader messageHeader, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.Stop();

        _diagnosticListener.Write(DiagnosticName.Event.ConsumerStop, messageHeader);
    }

    protected virtual void OnExceptionEvent(MessageHeader messageHeader, Exception ex, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.AddException(ex);
        activity.Stop();

        _diagnosticListener.Write(DiagnosticName.Event.ConsumerExecption, messageHeader);
    }

    protected virtual void OnFallbackStartEvent(MessageHeader messageHeader, BasicDeliverEventArgs eventArgs, string queue, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.Start();
    }

    protected virtual void OnFallbackEndEvent(MessageHeader messageHeader, ConsumerState fallbackState, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.AddTag("state", fallbackState);
        activity.Stop();
    }

    protected virtual void OnFallbackExceptionEvent(MessageHeader messageHeader, Exception ex, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.AddException(ex);
    }
}
