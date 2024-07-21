// <copyright file="DynamicConsumer.cs" company="Maomi">
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
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Dynamic consumer host.<br />
/// 动态消费者服务.
/// </summary>
public class DynamicConsumer : ConsumerBaseService, IDynamicConsumer
{
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly ConnectionPool _connectionPool;
    protected readonly IConnection _connection;

    protected readonly ILogger<DynamicConsumer> _logger;

    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;

    private readonly ConcurrentDictionary<string, ConsumerChannel> _consumers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicConsumer"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="connectionPool"></param>
    /// <param name="logger"></param>
    public DynamicConsumer(
        IServiceProvider serviceProvider,
        ServiceFactory serviceFactory,
        ConnectionPool connectionPool,
        ILogger<DynamicConsumer> logger)
        : base(serviceProvider, serviceFactory)
    {
        _connectionPool = connectionPool;
        _connection = _connectionPool.Get().Connection;
        _consumers = new();
        _logger = logger;

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
    }

    /// <inheritdoc/>
    public async Task<bool> StartAsync<TConsumer, TEvent>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
        where TEvent : class
        where TConsumer : class, IConsumer<TEvent>
    {
        if (_consumers.ContainsKey(consumerOptions.Queue))
        {
            return false;
        }

        var consummerChannel = await _connection.CreateChannelAsync();
        await InitQueueAsync(consummerChannel, consumerOptions);

        var messageConsumer = await InitConsumer(
            consummerChannel,
            new ConsumerType
            {
                Queue = consumerOptions.Queue,
                Consumer = typeof(TConsumer),
                Event = typeof(TEvent)
            },
            consumerOptions);

        var exist = !_consumers.TryAdd(consumerOptions.Queue, new ConsumerChannel
        {
            Channel = consummerChannel,
            MessageConsumer = messageConsumer
        });
        if (exist)
        {
            consummerChannel.Dispose();
        }

        return exist;
    }

    /// <inheritdoc/>
    public async Task StopAsync(string queue, CancellationToken stoppingToken = default)
    {
        if (_consumers.TryGetValue(queue, out var consumerChannel))
        {
            consumerChannel.Channel.Dispose();
            _consumers.Remove(queue, out _);
        }

        await Task.CompletedTask;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    private class ConsumerChannel
    {
        public IChannel Channel { get; init; } = default!;

        public MessageConsumer MessageConsumer { get; init; } = default!;
    }
}
