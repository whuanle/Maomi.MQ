// <copyright file="DynamicConsumerHostedService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.EventBus;
using Maomi.MQ.Pool;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Dynamic consumer host.<br />
/// 动态消费者服务.
/// </summary>
public class DynamicConsumerService : ConsumerBaseService, IDynamicConsumer
{
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly IConsumerTypeProvider _consumerTypeProvider;
    protected readonly ConnectionPool _connectionPool;
    protected readonly ConnectionObject _connectionObject;

    private readonly ConcurrentDictionary<string, ConsumerTag> _consumers;

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
        : base(serviceFactory)
    {
        _connectionPool = connectionPool;
        _connectionObject = _connectionPool.Get();
        _consumers = new();
        _consumerTypeProvider = consumerTypeProvider;
    }

    public Task StartEventAsync<TMessage>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
        where TMessage : class
    {
        return StartAsync<EventBusConsumer<TMessage>, TMessage>(consumerOptions, stoppingToken);
    }

    public async Task Consumer<TMessage>(
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

        (string consumerTag, MessageConsumer consumer) = await CreateDynamicMessageConsumer(consummerChannel, consumerOptions, dynamicProxyConsumer);

        var isAdd = _consumers.TryAdd(consumerOptions.Queue, new ConsumerTag
        {
            Tag = consumerTag,
            MessageConsumer = consumer
        });

        if (!isAdd)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true);
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }
    }

    protected virtual async Task<(string ConsumerTag, MessageConsumer Consumer)> CreateDynamicMessageConsumer<TMessage>(IChannel consummerChannel, IConsumerOptions consumerOptions, DynamicProxyConsumer<TMessage> proxyConsumer)
        where TMessage : class
    {
        await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
        var consumer = new AsyncEventingBasicConsumer(consummerChannel);

        MessageConsumer messageConsumer = new MessageConsumer(_serviceFactory, consumerOptions, s => proxyConsumer);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, consumerOptions.Queue } };
            if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
            {
                loggerState.Add(DiagnosticName.Event.Id, eventId!);
            }

            using (_logger.BeginScope(loggerState))
            {
                await messageConsumer.ConsumerAsync<TMessage>(consummerChannel, eventArgs);
            }
        };

        var consumerTag = await consummerChannel.BasicConsumeAsync(
            queue: consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        return (consumerTag, messageConsumer);
    }

    /// <inheritdoc/>
    public async Task StartAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
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

        (string consumerTag, MessageConsumer consumer) = await CreateMessageConsumer(consummerChannel, typeof(TMessage), consumerOptions);

        var isAdd = _consumers.TryAdd(consumerOptions.Queue, new ConsumerTag
        {
            Tag = consumerTag,
            MessageConsumer = consumer
        });

        if (!isAdd)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true, stoppingToken);
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by dynamic consumer");
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(string queue, CancellationToken stoppingToken = default)
    {
        if (_consumers.TryGetValue(queue, out var messageConsumer))
        {
            await _connectionObject.DefaultChannel.BasicCancelAsync(messageConsumer.Tag, true);
            _consumers.Remove(queue, out _);
        }

        await Task.CompletedTask;
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
