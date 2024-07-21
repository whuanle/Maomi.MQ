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
using System.Reflection;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Consumer.
/// </summary>
public class MessageConsumer
{
    protected readonly TagList _tags;
    protected readonly Meter _consumerMeter;
    protected readonly Counter<int> _pullMessageCount;
    protected readonly Counter<int> _messageFaildCount;
    protected readonly Counter<int> _messageFallbackCount;
    protected readonly Histogram<int> _messageSize;

    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly ILogger<MessageConsumer> _logger;
    protected readonly IConsumerOptions _consumerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageConsumer"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="logger"></param>
    /// <param name="consumerOptions"></param>
    public MessageConsumer(
        IServiceProvider serviceProvider,
        ServiceFactory serviceFactory,
        ILogger<MessageConsumer> logger,
        IConsumerOptions consumerOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _mqOptions = serviceFactory.Options;

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _consumerOptions = consumerOptions;

        _tags = new TagList
        {
            { "AppName", _mqOptions.AppName },
            { "Queue", consumerOptions.Queue }
        };

        _consumerMeter = new("MaomiMQ.Consumer", Assembly.GetAssembly(typeof(DefaultMessagePublisher))!.GetName()!.Version!.ToString(), _tags);
        _pullMessageCount = _consumerMeter.CreateCounter<int>("maomimq.consumer.message.pull.count", null, "The number of messages pushed or pulled by the server", _tags);
        _messageFaildCount = _consumerMeter.CreateCounter<int>("maomimq.consumer.message.faild.count", null, "The total number of retries for processing messages", _tags);
        _messageFallbackCount = _consumerMeter.CreateCounter<int>("maomimq.consumer.message.fallback.count", null, "The number of times the compensation method is executed", _tags);
        _messageSize = _consumerMeter.CreateHistogram<int>("maomimq.consumer.message.received", "Byte", "The size of the received message", _tags);
    }

    public virtual async Task ConsumerAsync<TEvent>(IChannel channel, BasicDeliverEventArgs eventArgs)
        where TEvent : class
    {
        _pullMessageCount.Add(1, _tags);
        _messageSize.Record(eventArgs.Body.Length, _tags);

        object? eventId = "-1";
        object? publisher = "unknown";
        _ = eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out eventId);
        _ = eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Publisher, out publisher);

        var tags = new ActivityTagsCollection()
        {
            { DiagnosticName.Event.Queue, _consumerOptions.Queue },
            { DiagnosticName.Event.Id, eventId },
            { DiagnosticName.Event.Publisher, publisher },
            { DiagnosticName.Event.Consumer, _mqOptions.AppName }
        };

        using Activity? consumerActivity = _diagnosticsWriter.WriteStarted(
            DiagnosticName.Activity.Consumer,
            DateTimeOffset.Now,
            tags);

        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        var consumer = ioc.GetKeyedService<IConsumer<TEvent>>(_consumerOptions.Queue);
        if (consumer == null)
        {
            consumer = ioc.GetRequiredService<IConsumer<TEvent>>();
        }

        EventBody<TEvent>? eventBody = null;

        try
        {
            eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;
            tags[DiagnosticName.Event.Id] = eventBody.Id;
            tags[DiagnosticName.Event.Publisher] = eventBody.Publisher;
            tags[DiagnosticName.Event.CreationTime] = eventBody.CreationTime;
            consumerActivity?.SetTag(DiagnosticName.Event.Id, eventBody.Id);
            consumerActivity?.SetTag(DiagnosticName.Event.Publisher, eventBody.Publisher);
            consumerActivity?.SetTag(DiagnosticName.Event.CreationTime, eventBody.CreationTime);

            // Executed on the last retry.
            // 最后一次重试失败时执行.
            var fallbackPolicy = Policy<bool>
                .Handle<Exception>()
                .FallbackAsync(async (c) =>
                {
                    _messageFallbackCount.Add(1);
                    return await FallbackAsync(_consumerOptions, tags, consumer, eventBody);
                });

            int retryCount = 0;

            // Custom retry policy.
            // 自定义重试策略.
            AsyncRetryPolicy customRetryPolicy = await _policyFactory.CreatePolicy(_consumerOptions.Queue, eventBody.Id);

            var policyWrap = fallbackPolicy.WrapAsync(customRetryPolicy);

            var executeResult = await policyWrap.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref retryCount);
                var result = await ExecuteAndRetryAsync(tags, consumer, eventBody, retryCount);
                return result;
            });

            // The execution completed normally, or the FallbackAsync function was executed to compensate for the last retry.
            // 正常执行完成，或执行了 FallbackAsync 函数补偿最后一次重试.
            if (executeResult)
            {
                await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
                tags[DiagnosticName.Tag.ACK] = "ack";

                consumerActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            else
            {
                // Whether to put it back to the queue when the last retry fails.
                // 最后一次重试失败时，是否放回队列.
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.RetryFaildRequeue);
                tags[DiagnosticName.Tag.ACK] = "nack";
                tags[DiagnosticName.Tag.Requeue] = _consumerOptions.RetryFaildRequeue;

                consumerActivity?.SetStatus(ActivityStatusCode.Error, "Failed to consume message");
            }

            _diagnosticsWriter.WriteStopped(consumerActivity, DateTimeOffset.Now, tags);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while processing the message,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody?.Id);
            _diagnosticsWriter.WriteException(consumerActivity, ex);

            using Activity? retrykActivity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Retry, DateTimeOffset.Now, tags);

            try
            {
                await consumer.FaildAsync(ex, -1, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody?.Id);
                _diagnosticsWriter.WriteException(retrykActivity, ex);
            }
            finally
            {
                _diagnosticsWriter.WriteStopped(retrykActivity, DateTimeOffset.Now);
            }

            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.ExecptionRequeue);

            tags[DiagnosticName.Tag.ACK] = "nack";
            tags[DiagnosticName.Tag.Requeue] = _consumerOptions.RetryFaildRequeue;
            _diagnosticsWriter.WriteStopped(consumerActivity, DateTimeOffset.Now, tags);
        }
    }

    public virtual async Task<bool> ExecuteAndRetryAsync<TEvent>(ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody, int retryCount)
        where TEvent : class
    {
        using Activity? executekActivity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Execute, DateTimeOffset.Now, tags);

        try
        {
            await consumer.ExecuteAsync(eventBody);
            _diagnosticsWriter.WriteStopped(executekActivity, DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _diagnosticsWriter.WriteException(executekActivity, ex);
            _diagnosticsWriter.WriteStopped(executekActivity, DateTimeOffset.Now);

            using Activity? retrykActivity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Retry, DateTimeOffset.Now, tags);
            _diagnosticsWriter.WriteEvent(retrykActivity, DiagnosticName.Event.Retry, "retry.count", retryCount);

            // Each retry.
            // 每次失败时执行.
            try
            {
                _messageFaildCount.Add(1);
                await consumer.FaildAsync(ex, retryCount, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody.Id);
                _diagnosticsWriter.WriteException(retrykActivity, faildEx);
            }
            finally
            {
                _diagnosticsWriter.WriteStopped(retrykActivity, DateTimeOffset.Now);
            }

            throw;
        }

        return true;
    }

    public virtual async Task<bool> FallbackAsync<TEvent>(IConsumerOptions consumerOptions, ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody)
        where TEvent : class
    {
        using Activity? fallbackActivity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Fallback, DateTimeOffset.Now, tags);
        try
        {
            var fallbackResult = await consumer.FallbackAsync(eventBody);
            _diagnosticsWriter.WriteEvent(fallbackActivity, DiagnosticName.Event.FallbackCompleted, DiagnosticName.Tag.Status, fallbackResult);

            return fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while executing the FallbackAsync method,queue [{Queue}],id [{Id}].", consumerOptions.Queue, eventBody.Id);
            _diagnosticsWriter.WriteException(fallbackActivity, ex);
            return false;
        }
        finally
        {
            _diagnosticsWriter.WriteStopped(fallbackActivity, DateTimeOffset.Now, tags);
        }
    }
}
