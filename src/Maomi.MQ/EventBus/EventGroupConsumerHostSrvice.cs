// <copyright file="EventGroupConsumerHostSrvice.cs" company="Maomi">
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
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq.Expressions;
using System.Reflection;
using static Maomi.MQ.Diagnostics.DiagnosticName;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Queues in the same group are executed on a channel.<br />
/// 将同一分组下的队列放到一个通道下执行.
/// </summary>
public partial class EventGroupConsumerHostService : ConsumerBaseHostSrvice
{
    protected readonly EventGroupInfo _eventGroupInfo;

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
        ILogger<ConsumerBaseHostSrvice> logger,
        IRetryPolicyFactory policyFactory,
        IWaitReadyFactory waitReadyFactory,
        EventGroupInfo eventGroupInfo)
        : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory)
    {
        _eventGroupInfo = eventGroupInfo;
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
    }

    /// <inheritdoc/>
    protected override async Task WaitReadyAsync()
    {
        if (_connectionOptions.AutoQueueDeclare)
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    foreach (var item in _eventGroupInfo.EventInfos)
                    {
                        Dictionary<string, object> arguments = new();
                        if (!string.IsNullOrEmpty(item.Value.DeadQueue))
                        {
                            arguments.Add("x-dead-letter-exchange", item.Value.DeadQueue);
                        }

                        await channel.QueueDeclareAsync(
                            queue: item.Value.Queue,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: arguments);

                        _logger.LogDebug("Declared queue [{Queue}].", item.Value.Queue);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        using IConnection connection = await _connectionFactory.CreateConnectionAsync();
        using IChannel channel = await connection.CreateChannelAsync();

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: GetQos(), global: false);

        foreach (var item in _eventGroupInfo.EventInfos)
        {
            var consumer = new EventingBasicConsumer(channel);
            var consumerHandler = BuildConsumerHandler(item.Value.EventType);

            consumer.Received += async (sender, eventArgs) =>
            {
                ConsumerOptions consumerOptions = new()
                {
                    Queue = item.Value.Queue,
                    ExecptionRequeue = item.Value.ExecptionRequeue,
                    Qos = item.Value.Qos,
                    RetryFaildRequeue = item.Value.RetryFaildRequeue
                };

                Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, consumerOptions.Queue } };
                if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
                {
                    loggerState.Add(DiagnosticName.Event.Id, eventId!);
                }

                using (_logger.BeginScope(loggerState))
                {
                    await consumerHandler(this, consumerOptions, channel, eventArgs)!;
                }
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

    private ushort GetQos()
    {
        // If each queue in a packet is configured with a different Qos, calculate the Qos based on the average value.
        var qos = (ushort)_eventGroupInfo.EventInfos.Average(x => x.Value.Qos);
        if (qos <= 0)
        {
            qos = 1;
        }

        return qos;
    }
}

/// <summary>
/// Queues in the same group are executed on a channel.<br />
/// 将同一分组下的队列放到一个通道下执行.
/// </summary>
public partial class EventGroupConsumerHostService
{
    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(EventGroupConsumerHostService)
        .GetMethod(nameof(ConsumerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// ConsumerHandler.
    /// </summary>
    /// <param name="hostService"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="channel"></param>
    /// <param name="eventArgs"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected delegate Task ConsumerHandler(EventGroupConsumerHostService hostService, ConsumerOptions consumerOptions, IChannel channel, BasicDeliverEventArgs eventArgs);

    /// <summary>
    /// Build delegate.
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns>Delegate.</returns>
    protected ConsumerHandler BuildConsumerHandler(Type eventType)
    {
        ParameterExpression consumer = Expression.Variable(typeof(EventGroupConsumerHostService), "consumer");
        ParameterExpression channel = Expression.Parameter(typeof(IChannel), "channel");
        ParameterExpression consumerOptions = Expression.Parameter(typeof(ConsumerOptions), "consumerOptions");
        ParameterExpression eventArgs = Expression.Parameter(typeof(BasicDeliverEventArgs), "eventArgs");
        MethodCallExpression method = Expression.Call(
            consumer,
            ConsumerMethod.MakeGenericMethod(eventType),
            consumerOptions,
            channel,
            eventArgs);

        return Expression.Lambda<ConsumerHandler>(method, consumer, consumerOptions, channel, eventArgs).Compile();
    }
}