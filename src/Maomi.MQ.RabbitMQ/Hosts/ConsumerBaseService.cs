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
    protected delegate Task<string> CreateConsumerHandler(ConsumerBaseService consumer, IChannel consummerChannel, Type messageType, IConsumerOptions consumerOptions);

    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(ConsumerBaseService)
        .GetMethod(nameof(ConsumerBaseService.CreateMessageConsumer), BindingFlags.Instance | BindingFlags.Public)!;

    protected readonly ServiceFactory _serviceFactory;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly ILogger _logger;

    protected ConsumerBaseService(ServiceFactory serviceFactory, ILoggerFactory loggerFactory)
    {
        _serviceFactory = serviceFactory;
        _serviceProvider = serviceFactory.ServiceProvider;
        _mqOptions = _serviceFactory.Options;

        _logger = loggerFactory.CreateLogger(DiagnosticName.Consumer);
    }

    /// <summary>
    /// Create queue and bind properties, create exchange and bind RoutingKey.<br />
    /// 创建队列和绑定属性，创建交换器和绑定 RoutingKey.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="consumerOptions"></param>
    /// <returns><see cref="Task"/>.</returns>
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

    /// <summary>
    /// Create a consumer for the queue, specify how to consume.<br />
    /// 为队列创建消费者，指定如何进行消费.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="consummerChannel"></param>
    /// <param name="messageType"></param>
    /// <param name="consumerOptions"></param>
    /// <returns>Consumer tag.</returns>
    protected virtual async Task<string> CreateMessageConsumer<TMessage>(IChannel consummerChannel, Type messageType, IConsumerOptions consumerOptions)
    where TMessage : class
    {
        await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
        var consumer = new AsyncEventingBasicConsumer(consummerChannel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new()
            {
                { "Queue", consumerOptions.Queue },
                { "Exchange", eventArgs.Exchange },
                { "RoutingKey", eventArgs.RoutingKey },
                { "ConsumerTag", eventArgs.ConsumerTag },
                { "DeliveryTag", eventArgs.DeliveryTag },
                { "Redelivered", eventArgs.Redelivered },
                { "MessageId", eventArgs.BasicProperties.MessageId ?? string.Empty }
            };

            using (_logger.BeginScope(loggerState))
            {
                using var scope = _serviceProvider.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
                {
                    return s.GetService<IConsumer<TMessage>>()!;
                });

                await messageConsumer.ConsumerAsync<TMessage>(consummerChannel, eventArgs);
            }
        };

        consummerChannel.BasicReturnAsync += async (sender, args) =>
        {
            var breakdown = _serviceProvider.GetRequiredService<IBreakdown>();
            await breakdown.BasicReturn(sender, args);
        };

        var consumerTag = await consummerChannel.BasicConsumeAsync(
            queue: consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        return consumerTag;
    }

    /// <summary>
    /// Build delegate.
    /// </summary>
    /// <param name="messageType"></param>
    /// <returns>Delegate.</returns>
    protected virtual CreateConsumerHandler BuildCreateConsumerHandler(Type messageType)
    {
        ParameterExpression service = Expression.Variable(typeof(ConsumerBaseService), "service");
        ParameterExpression channel = Expression.Parameter(typeof(IChannel), "channel");
        ParameterExpression type = Expression.Parameter(typeof(Type), "messageType");
        ParameterExpression consumerOptions = Expression.Parameter(typeof(IConsumerOptions), "channel");
        MethodCallExpression method = Expression.Call(
            service,
            ConsumerMethod.MakeGenericMethod(messageType),
            channel,
            type,
            consumerOptions);

        return Expression.Lambda<CreateConsumerHandler>(method, service, type, consumerOptions).Compile();
    }
}
