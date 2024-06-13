// <copyright file="ConsumerBaseHostService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.
/// </summary>
public partial class ConsumerBaseHostService : BackgroundService
{
    protected readonly TaskCompletionSource _taskCompletionSource;
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly IConnectionFactory _connectionFactory;
    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IWaitReadyFactory _waitReadyFactory;
    protected readonly ILogger<ConsumerBaseHostService> _logger;

    private readonly IReadOnlyList<ConsumerType> _consumerTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBaseHostService"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="logger"></param>
    /// <param name="consumerTypes"></param>
    public ConsumerBaseHostService(
        IServiceProvider serviceProvider,
        ServiceFactory serviceFactory,
        ILogger<ConsumerBaseHostService> logger,
        IReadOnlyList<ConsumerType> consumerTypes)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _mqOptions = serviceFactory.Options;
        _connectionFactory = serviceFactory.ConnectionFactory;

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _waitReadyFactory = serviceFactory.WaitReadyFactory;
        _taskCompletionSource = new();
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
        _consumerTypes = consumerTypes;
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

    protected virtual async Task WaitReadyAsync()
    {
        var pool = _serviceProvider.GetRequiredService<ConnectionPool>();
        using var connectionObject = pool.CreateAutoReturn();
        var channel = connectionObject.Channel;

        foreach (var consumerType in _consumerTypes)
        {
            Dictionary<string, object> arguments = new();
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: consumerType.Queue);

            if (consumerOptions.AutoQueueDeclare == AutoQueueDeclare.Disable)
            {
                continue;
            }
            else if (!_mqOptions.AutoQueueDeclare && consumerOptions.AutoQueueDeclare == AutoQueueDeclare.None)
            {
                continue;
            }

            if (consumerOptions.Expiration != default)
            {
                arguments.Add("x-expires", consumerOptions.Expiration);
            }

            if (!string.IsNullOrEmpty(consumerOptions.DeadQueue))
            {
                arguments.Add("x-dead-letter-exchange", string.Empty);
                arguments.Add("x-dead-letter-routing-key", consumerOptions.DeadQueue);
            }

            if (consumerOptions.RetryFaildRequeue && !string.IsNullOrEmpty(consumerOptions.DeadQueue))
            {
                _logger.LogWarning(
                    "Queue name [{Queue}],because (RetryFaildRequeue == true) is configured, queue [{DeadQueue}] does not take effect.",
                    consumerOptions.Queue,
                    consumerOptions.DeadQueue);
            }

            // Create queues based on consumers.
            // 根据消费者创建队列.
            await channel.QueueDeclareAsync(
                queue: consumerOptions.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            if (!string.IsNullOrEmpty(consumerOptions.BindExchange))
            {
                await channel.ExchangeDeclareAsync(consumerOptions.BindExchange, ExchangeType.Fanout);
                await channel.QueueBindAsync(exchange: consumerOptions.BindExchange, queue: consumerOptions.Queue, routingKey: string.Empty);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IConnection connection = await _connectionFactory.CreateConnectionAsync();
        using IChannel channel = await connection.CreateChannelAsync();

        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        List<int> qos = new();
        Dictionary<string, EventingBasicConsumer> consumers = new();

        int consumerCount = 0;
        foreach (var consumerType in _consumerTypes)
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: consumerType.Queue);
            if (consumerType.Consumer.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEmptyConsumer<>)))
            {
                continue;
            }

            consumerCount++;
            qos.Add(consumerOptions.Qos);

            var consumer = new EventingBasicConsumer(channel);
            consumers.Add(consumerType.Queue, consumer);
            var consumerHandler = BuildConsumerHandler(consumerType.Event);

            consumer.Received += async (sender, eventArgs) =>
            {
                Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, consumerOptions.Queue } };
                if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
                {
                    loggerState.Add(DiagnosticName.Event.Id, eventId!);
                }

                using (_logger.BeginScope(loggerState))
                {
                    await consumerHandler(this, channel, consumerOptions, eventArgs);
                }
            };
        }

        if (consumerCount == 0)
        {
            return;
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: (ushort)qos.Average(), global: false);

        foreach (var consumer in consumers)
        {
            await channel.BasicConsumeAsync(
                queue: consumer.Key,
                autoAck: false,
                consumer: consumer.Value);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }

    protected virtual async Task ConsumerAsync<TEvent>(IChannel channel, IConsumerOptions consumerOptions, BasicDeliverEventArgs eventArgs)
        where TEvent : class
    {
        object? eventId = "-1";
        object? publisher = "unknown";
        _ = eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out eventId);
        _ = eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Publisher, out publisher);

        var tags = new ActivityTagsCollection()
        {
            { DiagnosticName.Event.Queue, consumerOptions.Queue },
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

        var consumer = ioc.GetRequiredKeyedService<IConsumer<TEvent>>(consumerOptions.Queue);
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

    protected virtual async Task<bool> ExecuteAndRetryAsync<TEvent>(IConsumerOptions consumerOptions, ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody, int retryCount)
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

    protected virtual async Task<bool> FallbackAsync<TEvent>(IConsumerOptions consumerOptions, ActivityTagsCollection tags, IConsumer<TEvent> consumer, EventBody<TEvent> eventBody)
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

/// <summary>
/// Base consumer service.
/// </summary>
public partial class ConsumerBaseHostService
{
    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(ConsumerBaseHostService)
        .GetMethod(nameof(ConsumerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected delegate Task ConsumerHandler(ConsumerBaseHostService hostService, IChannel channel, IConsumerOptions consumerOptions, BasicDeliverEventArgs eventArgs);

    /// <summary>
    /// Build delegate.
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns>Delegate.</returns>
    protected virtual ConsumerHandler BuildConsumerHandler(Type eventType)
    {
        ParameterExpression consumer = Expression.Variable(typeof(ConsumerBaseHostService), "consumer");
        ParameterExpression channel = Expression.Parameter(typeof(IChannel), "channel");
        ParameterExpression consumerOptions = Expression.Parameter(typeof(IConsumerOptions), "consumerOptions");
        ParameterExpression eventArgs = Expression.Parameter(typeof(BasicDeliverEventArgs), "eventArgs");
        MethodCallExpression method = Expression.Call(
            consumer,
            ConsumerMethod.MakeGenericMethod(eventType),
            channel,
            consumerOptions,
            eventArgs);

        return Expression.Lambda<ConsumerHandler>(method, consumer, channel, consumerOptions, eventArgs).Compile();
    }
}
