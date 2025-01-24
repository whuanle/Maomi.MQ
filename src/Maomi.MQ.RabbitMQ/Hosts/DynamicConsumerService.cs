// <copyright file="DynamicConsumerService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.EventBus;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Dynamic consumer host.<br />
/// 动态消费者服务.
/// </summary>
public class DynamicConsumerService : ConsumerBaseService, IDynamicConsumer
{
    protected readonly IConsumerTypeProvider _consumerTypeProvider;
    protected readonly ConnectionPool _connectionPool;
    protected readonly ConnectionObject _connectionObject;

    protected readonly ConcurrentDictionary<string, string> _consumers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicConsumerService"/> class.
    /// </summary>
    /// <param name="serviceFactory"></param>
    /// <param name="connectionPool"></param>
    /// <param name="consumerTypeProvider"></param>
    public DynamicConsumerService(
        ServiceFactory serviceFactory,
        ConnectionPool connectionPool,
        IConsumerTypeProvider consumerTypeProvider)
        : base(serviceFactory, serviceFactory.ServiceProvider.GetRequiredService<ILoggerFactory>())
    {
        _connectionPool = connectionPool;
        _connectionObject = _connectionPool.Get();
        _consumers = new();
        _consumerTypeProvider = consumerTypeProvider;
    }

    /// <inheritdoc />
    public Task<string> ConsumerAsync<TMessage>(IConsumerOptions consumerOptions)
        where TMessage : class
    {
        return ConsumerAsync<EventBusConsumer<TMessage>, TMessage>(consumerOptions);
    }

    /// <inheritdoc />
    public async Task<string> ConsumerAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions)
    where TMessage : class
    where TConsumer : class, IConsumer<TMessage>
    {
        var existConsumer = _consumerTypeProvider.FirstOrDefault(x => x.Queue == consumerOptions.Queue);
        if (existConsumer != null)
        {
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by consumer [{existConsumer.Event.Name}]");
        }

        if (_consumers.ContainsKey(consumerOptions.Queue))
        {
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }

        var consummerChannel = _connectionObject.DefaultChannel;
        await InitQueueAsync(consummerChannel, consumerOptions);

        var createConsumer = BuildCreateConsumerHandler(typeof(TMessage));
        var consumerTag = await createConsumer(this, consummerChannel, typeof(TMessage), consumerOptions);

        var isAdd = _consumers.TryAdd(consumerOptions.Queue, consumerTag);

        if (!isAdd)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true);
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }

        return consumerTag;
    }

    /// <inheritdoc />
    public async Task<string> ConsumerAsync<TMessage>(
        IConsumerOptions consumerOptions,
        ConsumerExecuteAsync<TMessage> execute,
        ConsumerFaildAsync<TMessage>? faild = null,
        ConsumerFallbackAsync<TMessage>? fallback = null)
        where TMessage : class
    {
        var existConsumer = _consumerTypeProvider.FirstOrDefault(x => x.Queue == consumerOptions.Queue);
        if (existConsumer != null)
        {
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by consumer [{existConsumer.Event.Name}]");
        }

        if (_consumers.ContainsKey(consumerOptions.Queue))
        {
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }

        var dynamicProxyConsumer = new DynamicProxyConsumer<TMessage>(execute, faild, fallback);
        var consummerChannel = _connectionObject.DefaultChannel;
        await InitQueueAsync(consummerChannel, consumerOptions);

        string consumerTag = await CreateDynamicMessageConsumer(consummerChannel, consumerOptions, dynamicProxyConsumer);

        var isAdd = _consumers.TryAdd(consumerOptions.Queue, consumerTag);

        if (!isAdd)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true);
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }

        return consumerTag;
    }

    /// <inheritdoc />
    public virtual async Task StopConsumerAsync(string queue)
    {
        if (_consumers.TryGetValue(queue, out var consumerTag))
        {
            await _connectionObject.DefaultChannel.BasicCancelAsync(consumerTag, true);
            _consumers.Remove(queue, out _);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task StopConsumerTagAsync(string consumerTag)
    {
        var kv = _consumers.FirstOrDefault(x => x.Value == consumerTag);

        if (kv.Value != null)
        {
            _consumers.Remove(kv.Key, out _);
        }

        await _connectionObject.DefaultChannel.BasicCancelAsync(consumerTag, true);

        await Task.CompletedTask;
    }

    protected virtual async Task<string> CreateDynamicMessageConsumer<TMessage>(IChannel consummerChannel, IConsumerOptions consumerOptions, DynamicProxyConsumer<TMessage> proxyConsumer)
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

                MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s => proxyConsumer);
                await messageConsumer.ConsumerAsync<TMessage>(consummerChannel, eventArgs);
            }
        };

        var consumerTag = await consummerChannel.BasicConsumeAsync(
            queue: consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        return consumerTag;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private class ConsumerTag
    {
        public string Tag { get; init; } = default!;

        public MessageConsumer MessageConsumer { get; init; } = default!;
    }
}
