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
/// Base consumer host service.
/// </summary>
public abstract class ConsumerBaseService : BackgroundService
{
    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(MessageConsumer)
        .GetMethod(nameof(MessageConsumer.ConsumerAsync), BindingFlags.Instance | BindingFlags.Public)!;

    protected readonly ServiceFactory _serviceFactory;

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly ILogger _logger;

    protected ConsumerBaseService(ServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
        _serviceProvider = serviceFactory.ServiceProvider;
        _mqOptions = _serviceFactory.Options;

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(DiagnosticName.ConsumerName);
    }

    protected virtual async Task InitQueueAsync(IChannel channel, IConsumerOptions consumerOptions)
    {
        Dictionary<string, object?> arguments = new();

        if (consumerOptions.AutoQueueDeclare == AutoQueueDeclare.Disable)
        {
            return;
        }
        else if (!_mqOptions.AutoQueueDeclare && consumerOptions.AutoQueueDeclare == AutoQueueDeclare.None)
        {
            return;
        }

        /*
         https://www.rabbitmq.com/docs/stomp#queue-parameters
         */

        if (consumerOptions.Expiration != default)
        {
            arguments.Add("x-expires", consumerOptions.Expiration);
        }

        if (!string.IsNullOrEmpty(consumerOptions.DeadRoutingKey))
        {
            arguments.Add("x-dead-letter-exchange", consumerOptions.DeadExchange ?? string.Empty);
            arguments.Add("x-dead-letter-routing-key", consumerOptions.DeadRoutingKey);
        }

        if (consumerOptions.RetryFaildRequeue && !string.IsNullOrEmpty(consumerOptions.DeadRoutingKey))
        {
            _logger.LogWarning(
                "Queue name [{Queue}],because (RetryFaildRequeue == true) is configured, queue [{DeadQueue}] does not take effect.",
                consumerOptions.Queue,
                consumerOptions.DeadRoutingKey);
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
            ArgumentNullException.ThrowIfNull(consumerOptions.ExchangeType, nameof(consumerOptions.ExchangeType));
            await channel.ExchangeDeclareAsync(consumerOptions.BindExchange, consumerOptions.ExchangeType);
            await channel.QueueBindAsync(exchange: consumerOptions.BindExchange, queue: consumerOptions.Queue, routingKey: consumerOptions.RoutingKey ?? consumerOptions.Queue);
        }
    }

    protected virtual async Task<(string ConsumerTag, MessageConsumer Consumer)> CreateMessageConsumer(IChannel consummerChannel, Type eventType, IConsumerOptions consumerOptions)
    {
        await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
        var consumer = new AsyncEventingBasicConsumer(consummerChannel);

        var consumerHandler = BuildConsumerHandler(eventType);
        MessageConsumer messageConsumer = new MessageConsumer(_serviceFactory, consumerOptions, s =>
        {
            return s.GetRequiredService(typeof(IConsumer<>).MakeGenericType(eventType));
        });

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, consumerOptions.Queue } };
            if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
            {
                loggerState.Add(DiagnosticName.Event.Id, eventId!);
            }

            using (_logger.BeginScope(loggerState))
            {
                await consumerHandler(messageConsumer, consummerChannel, eventArgs);
            }
        };

        var consumerTag = await consummerChannel.BasicConsumeAsync(
            queue: consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        return (consumerTag, messageConsumer);
    }

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
