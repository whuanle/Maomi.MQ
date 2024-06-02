// <copyright file="ConsumerBaseHostSrvice.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600

using Maomi.MQ.Defaults;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace Maomi.MQ;

/// <summary>
/// Base consumer service.
/// </summary>
public abstract class ConsumerBaseHostSrvice : BackgroundService
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly DefaultMqOptions _connectionOptions;
    protected readonly IConnectionFactory _connectionFactory;
    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IWaitReadyFactory _waitReadyFactory;
    protected readonly ILogger<ConsumerBaseHostSrvice> _logger;

    protected readonly TaskCompletionSource _taskCompletionSource;
    protected readonly DiagnosticsWriter _diagnosticsWriter = new ConsumerDiagnosticsWriter();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBaseHostSrvice"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="waitReadyFactory"></param>
    public ConsumerBaseHostSrvice(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<ConsumerBaseHostSrvice> logger,
        IRetryPolicyFactory policyFactory,
        IWaitReadyFactory waitReadyFactory)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionOptions = connectionOptions;
        _connectionFactory = connectionOptions.ConnectionFactory;

        _policyFactory = policyFactory;
        _waitReadyFactory = waitReadyFactory;
        _taskCompletionSource = new();
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
    }

    /// <inheritdoc />.
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await WaitReadyAsync();
            _taskCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue.");
            _taskCompletionSource?.TrySetException(ex);
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Consumer messsage.
    /// </summary>
    /// <typeparam name="TEvent">Event model.</typeparam>
    /// <param name="channel"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="eventArgs"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual async Task ConsumerAsync<TEvent>(IChannel channel, ConsumerOptions consumerOptions, BasicDeliverEventArgs eventArgs)
        where TEvent : class
    {
        object? eventId = "-1";
        _ = eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out eventId);

        var tags = new ActivityTagsCollection() { { DiagnosticName.Event.Queue, consumerOptions.Queue }, { DiagnosticName.Event.Id, eventId } };

        using Activity? consumerActivity = _diagnosticsWriter.WriteStarted(
            DiagnosticName.Activity.Consumer,
            DateTimeOffset.Now,
            tags);

        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        var consumer = ioc.GetRequiredKeyedService<IConsumer<TEvent>>(consumerOptions.Queue);
        EventBody<TEvent>? eventBody = null;

        try
        {
            eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;
            tags[DiagnosticName.Event.Id] = eventBody.Id;
            tags[DiagnosticName.Event.CreateTime] = eventBody.CreateTime;
            consumerActivity?.SetTag(DiagnosticName.Event.Id, eventBody.Id);
            consumerActivity?.SetTag(DiagnosticName.Event.CreateTime, eventBody.CreateTime);

            // Executed on the last retry.
            // 最后一次重试失败时执行.
            var fallbackPolicy = Policy<bool>
                .Handle<Exception>()
                .FallbackAsync(async (c) =>
                {
                    return await FallbackAsync(consumerOptions, tags, consumer, eventBody);
                });

            int retryCount = 0;

            // Custom retry policy.
            // 自定义重试策略.
            AsyncRetryPolicy customRetryPolicy = await _policyFactory.CreatePolicy(consumerOptions.Queue, eventBody.Id);

            var policyWrap = fallbackPolicy.WrapAsync(customRetryPolicy);

            var executeResult = await policyWrap.ExecuteAsync(async () =>
            {
                Interlocked.Increment(ref retryCount);
                var result = await ExecuteAndRetryAsync(consumerOptions, tags, consumer, eventBody, retryCount);
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
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: consumerOptions.RetryFaildRequeue);
                tags[DiagnosticName.Tag.ACK] = "nack";
                tags[DiagnosticName.Tag.Requeue] = consumerOptions.RetryFaildRequeue;

                consumerActivity?.SetStatus(ActivityStatusCode.Error, "Failed to consume message");
            }

            _diagnosticsWriter.WriteStopped(consumerActivity, DateTimeOffset.Now, tags);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while processing the message,queue [{Queue}],id [{Id}].", consumerOptions.Queue, eventBody?.Id);
            _diagnosticsWriter.WriteException(consumerActivity, ex);

            using Activity? retrykActivity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Retry, DateTimeOffset.Now, tags);

            try
            {
                await consumer.FaildAsync(ex, -1, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", consumerOptions.Queue, eventBody?.Id);
                _diagnosticsWriter.WriteException(retrykActivity, ex);
            }
            finally
            {
                _diagnosticsWriter.WriteStopped(retrykActivity, DateTimeOffset.Now);
            }

            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: consumerOptions.ExecptionRequeue);

            tags[DiagnosticName.Tag.ACK] = "nack";
            tags[DiagnosticName.Tag.Requeue] = consumerOptions.RetryFaildRequeue;
            _diagnosticsWriter.WriteStopped(consumerActivity, DateTimeOffset.Now, tags);
        }
    }

    /// <summary>
    /// Wait queue ready.<br />
    /// 等待服务就绪.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task WaitReadyAsync();

    protected virtual async Task<bool> ExecuteAndRetryAsync<TEvent>(ConsumerOptions consumerOptions, ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody, int retryCount)
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
                await consumer.FaildAsync(ex, retryCount, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", consumerOptions.Queue, eventBody.Id);
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

    protected virtual async Task<bool> FallbackAsync<TEvent>(ConsumerOptions consumerOptions, ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody)
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