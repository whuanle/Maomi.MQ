// <copyright file="ConsumerBaseService.cs" company="Maomi">
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq.Expressions;
using System.Reflection;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.
/// </summary>
public abstract class ConsumerBaseService : BackgroundService
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ServiceFactory _serviceFactory;
    protected readonly MqOptions _mqOptions;

    private readonly ILogger<ConsumerBaseService> _logger;

    protected virtual async Task InitQueueAsync(IChannel channel, IConsumerOptions consumerOptions)
    {
        Dictionary<string, object> arguments = new();

        if (consumerOptions.AutoQueueDeclare == AutoQueueDeclare.Disable)
        {
            return;
        }
        else if (!_mqOptions.AutoQueueDeclare && consumerOptions.AutoQueueDeclare == AutoQueueDeclare.None)
        {
            return;
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

    protected virtual async Task<MessageConsumer> InitConsumer(IChannel consummerChannel, ConsumerType consumerType, IConsumerOptions consumerOptions)
    {
        await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
        var consumer = new EventingBasicConsumer(consummerChannel);

        var consumerHandler = BuildConsumerHandler(consumerType.Event);
        MessageConsumer messageConsumer = new MessageConsumer(_serviceProvider, _serviceFactory, _serviceProvider.GetRequiredService<ILogger<MessageConsumer>>(), consumerOptions);

        consumer.Received += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, consumerType.Queue } };
            if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
            {
                loggerState.Add(DiagnosticName.Event.Id, eventId!);
            }

            using (_logger.BeginScope(loggerState))
            {
                await consumerHandler(messageConsumer, consummerChannel, eventArgs);
            }
        };

        await consummerChannel.BasicConsumeAsync(
            queue: consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        return messageConsumer;
    }

    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(MessageConsumer)
        .GetMethod(nameof(MessageConsumer.ConsumerAsync), BindingFlags.Instance | BindingFlags.Public)!;

    protected ConsumerBaseService(IServiceProvider serviceProvider, ServiceFactory serviceFactory)
    {
        _serviceProvider = serviceProvider;
        _serviceFactory = serviceFactory;
        _mqOptions = _serviceFactory.Options;
        _logger = _serviceProvider.GetRequiredService<ILogger<ConsumerBaseService>>();
    }

    protected delegate Task ConsumerHandler(MessageConsumer hostService, IChannel channel, BasicDeliverEventArgs eventArgs);

    /// <summary>
    /// Build delegate.
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns>Delegate.</returns>
    protected virtual ConsumerHandler BuildConsumerHandler(Type eventType)
    {
        ParameterExpression consumer = Expression.Variable(typeof(MessageConsumer), "consumer");
        ParameterExpression channel = Expression.Parameter(typeof(IChannel), "channel");
        ParameterExpression eventArgs = Expression.Parameter(typeof(BasicDeliverEventArgs), "eventArgs");
        MethodCallExpression method = Expression.Call(
            consumer,
            ConsumerMethod.MakeGenericMethod(eventType),
            channel,
            eventArgs);

        return Expression.Lambda<ConsumerHandler>(method, consumer, channel, eventArgs).Compile();
    }
}
