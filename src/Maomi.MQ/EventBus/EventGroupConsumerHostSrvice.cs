// <copyright file="EventGroupConsumerHostSrvice.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq.Expressions;
using System.Reflection;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Queues in the same group are executed on a channel.<br />
/// 将同一分组下的队列放到一个通道下执行.
/// </summary>
public partial class EventGroupConsumerHostService : BackgroundService
{
    private static readonly MethodInfo ConsumerMethod = typeof(EventGroupConsumerHostService)
        .GetMethod("ConsumerAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IServiceProvider _serviceProvider;
    private readonly DefaultMqOptions _connectionOptions;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IRetryPolicyFactory _policyFactory;
    private readonly ILogger<EventGroupConsumerHostService> _logger;
    private readonly EventGroupInfo _eventGroupInfo;
    private readonly IWaitReadyFactory _waitReadyFactory;
    private readonly TaskCompletionSource _taskCompletionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGroupConsumerHostService"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="waitReadyFactory"></param>
    /// <param name="eventGroupInfo"></param>
    /// <param name="circuitBreakerFactory"></param>
    public EventGroupConsumerHostService(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<EventGroupConsumerHostService> logger,
        IRetryPolicyFactory policyFactory,
        IWaitReadyFactory waitReadyFactory,
        EventGroupInfo eventGroupInfo)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionOptions = connectionOptions;
        _connectionFactory = connectionOptions.ConnectionFactory;
        _policyFactory = policyFactory;
        _waitReadyFactory = waitReadyFactory;
        _eventGroupInfo = eventGroupInfo;

        _taskCompletionSource = new();
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
    }

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    foreach (var item in _eventGroupInfo.EventInfos)
                    {
                        await channel.QueueDeclareAsync(
                            queue: item.Value.Queue,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

                        _logger.LogDebug("Declared queue [{Queue}].", item.Value.Queue);
                    }
                }
            }

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

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        using IConnection connection = await _connectionFactory.CreateConnectionAsync();
        using IChannel channel = await connection.CreateChannelAsync();

        // If each queue in a packet is configured with a different Qos, calculate the Qos based on the average value.
        var qos = (ushort)_eventGroupInfo.EventInfos.Average(x => x.Value.Qos);
        if (qos <= 0)
        {
            qos = 1;
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: qos, global: false);

        foreach (var item in _eventGroupInfo.EventInfos)
        {
            var consumer = new EventingBasicConsumer(channel);
            var consumerHandler = BuildConsumerHandler(item.Value.EventType);

            consumer.Received += async (sender, eventArgs) =>
            {
                await (Task)consumerHandler.DynamicInvoke(this, new object[] { item.Value, channel, eventArgs })!;
            };

            await channel.BasicConsumeAsync(
                queue: item.Value.Queue,
                autoAck: false,
                consumer: consumer);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }

    /// <summary>
    /// Consumer messsage.
    /// </summary>
    /// <typeparam name="TEvent">Event model.</typeparam>
    /// <param name="eventInfo"></param>
    /// <param name="channel"></param>
    /// <param name="eventArgs"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual async Task ConsumerAsync<TEvent>(EventInfo eventInfo, IChannel channel, BasicDeliverEventArgs eventArgs)
        where TEvent : class
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        var consumer = ioc.GetRequiredService<IConsumer<TEvent>>();

        EventBody<TEvent>? eventBody = null;

        try
        {
            eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;

            // Executed on the last retry.
            // 最后一次重试失败时执行.
            var fallbackPolicy = Policy<bool>
                .Handle<Exception>()
                .FallbackAsync(async (c) =>
                {
                    try
                    {
                        return await consumer.FallbackAsync(eventBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "An error occurred while executing the FallbackAsync method,queue [{Queue}],id [{Id}].", eventInfo.Queue, eventBody.Id);
                        return false;
                    }
                });

            // Each retry.
            // 每次失败时执行.
            int retryCount = 0;
            var retryEachPolicy = Policy.Handle<Exception>().RetryAsync(async (ex, count) =>
            {
                try
                {
                    retryCount++;
                    await consumer.FaildAsync(ex, retryCount, eventBody);
                }
                catch (Exception faildEx)
                {
                    _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", eventInfo.Queue, eventBody.Id);
                }
            });

            // Custom retry policy.
            // 自定义重试策略.
            var customRetryPolicy = await _policyFactory.CreatePolicy(eventInfo.Queue);

            var policyWrap = fallbackPolicy.WrapAsync(customRetryPolicy).WrapAsync(retryEachPolicy);

            var executeResult = await policyWrap.ExecuteAsync(async () =>
            {
                await consumer.ExecuteAsync(eventBody);
                return true;
            });

            // The execution completed normally, or the FallbackAsync function was executed to compensate for the last retry.
            // 正常执行完成，或执行了 FallbackAsync 函数补偿最后一次重试.
            if (executeResult)
            {
                await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                // Whether to put it back to the queue when the last retry fails.
                // 最后一次重试失败时，是否放回队列.
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: eventInfo.RetryFaildRequeue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while processing the message,queue [{Queue}],id [{Id}].", eventInfo.Queue, eventBody?.Id);

            try
            {
                await consumer.FaildAsync(ex, -1, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", eventInfo.Queue, eventBody?.Id);
            }

            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: eventInfo.ExecptionRequeue);
        }
    }
}

/// <summary>
/// Queues in the same group are executed on a channel.<br />
/// 将同一分组下的队列放到一个通道下执行.
/// </summary>
public partial class EventGroupConsumerHostService
{
    /// <summary>
    /// Build delegate.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>Delegate.</returns>
    protected static Delegate BuildConsumerHandler(Type type)
    {
        ParameterExpression consumer = Expression.Variable(typeof(EventGroupConsumerHostService), "coosumer");
        ParameterExpression eventInfo = Expression.Parameter(typeof(EventInfo), "eventInfo");
        ParameterExpression channel = Expression.Parameter(typeof(IChannel), "channel");
        ParameterExpression eventArgs = Expression.Parameter(typeof(BasicDeliverEventArgs), "eventArgs");

        MethodCallExpression method = Expression.Call(
            consumer,
            ConsumerMethod.MakeGenericMethod(type),
            new Expression[] { eventInfo, channel, eventArgs });

        return Expression.Lambda(method).Compile();
    }
}