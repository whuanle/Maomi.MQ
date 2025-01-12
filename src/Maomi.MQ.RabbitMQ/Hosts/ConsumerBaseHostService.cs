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
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.Initialize the queue and build the consumer application.<br />
/// 初始化队列和构建消费者程序.
/// </summary>
public partial class ConsumerBaseHostService : ConsumerBaseService
{
    protected readonly TaskCompletionSource _readyCompletionSource;
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly ConnectionPool _connectionPool;
    protected readonly IConnection _connection;

    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IWaitReadyFactory _waitReadyFactory;
    protected readonly ILogger<ConsumerBaseHostService> _logger;

    protected readonly IReadOnlyList<ConsumerType> _consumerTypes;
    protected readonly Dictionary<string, MessageConsumer> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBaseHostService"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="connectionPool"></param>
    /// <param name="logger"></param>
    /// <param name="consumerTypes"></param>
    public ConsumerBaseHostService(
        IServiceProvider serviceProvider,
        ServiceFactory serviceFactory,
        ConnectionPool connectionPool,
        ILogger<ConsumerBaseHostService> logger,
        IReadOnlyList<ConsumerType> consumerTypes)
        : base(serviceProvider, serviceFactory)
    {
        _connectionPool = connectionPool;
        _connection = _connectionPool.Get().Connection;

        _logger = logger;

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _waitReadyFactory = serviceFactory.WaitReadyFactory;
        _readyCompletionSource = new();
        _waitReadyFactory.AddTask(_readyCompletionSource.Task);
        _consumerTypes = consumerTypes;
    }

    /// <inheritdoc />.
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitReadyInitQueueAsync(_connection);
            _readyCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue.");
            _readyCompletionSource?.TrySetException(ex);
            throw;
        }

        int consumerCount = await WaitReadyConsumerAsync(_connection);

        if (consumerCount == 0)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }

    protected virtual async Task WaitReadyInitQueueAsync(IConnection connection)
    {
        using var channel = await connection.CreateChannelAsync();

        foreach (var consumerType in _consumerTypes)
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: consumerType.Queue);
            await InitQueueAsync(channel, consumerOptions);
        }
    }

    protected virtual async Task<int> WaitReadyConsumerAsync(IConnection connection)
    {
        int consumerCount = 0;

        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;
        foreach (var consumerType in _consumerTypes)
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: consumerType.Queue);
            if (consumerType.Consumer.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEmptyConsumer<>)))
            {
                continue;
            }

            consumerCount++;

            var consummerChannel = await connection.CreateChannelAsync();

            var messageConsumer = await InitConsumer(consummerChannel, consumerType, consumerOptions);
            _consumers.Add(consumerOptions.Queue, messageConsumer);
        }

        return consumerCount;
    }
}