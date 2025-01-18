// <copyright file="ConsumerHostedService.cs" company="Maomi">
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
public partial class ConsumerHostedService : ConsumerBaseService
{
    protected readonly TaskCompletionSource _readyCompletionSource;
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly ConnectionObject _connectionObject;

    protected readonly IMessageSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;
    protected readonly IWaitReadyFactory _waitReadyFactory;

    protected readonly IReadOnlyList<ConsumerType> _consumerTypes;
    protected readonly Dictionary<MessageConsumer, IChannel> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerHostedService"/> class.
    /// </summary>
    /// <param name="serviceFactory"></param>
    /// <param name="connectionPool"></param>
    /// <param name="consumerTypes"></param>
    public ConsumerHostedService(
        ServiceFactory serviceFactory,
        ConnectionPool connectionPool,
        IReadOnlyList<ConsumerType> consumerTypes)
        : base(serviceFactory)
    {
        _connectionObject = connectionPool.Get();

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _waitReadyFactory = serviceFactory.WaitReadyFactory;
        _consumerTypes = consumerTypes;

        _readyCompletionSource = new();
        _waitReadyFactory.AddTask(_readyCompletionSource.Task);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await WaitReadyInitQueueAsync();
            _readyCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue.");
            _readyCompletionSource?.TrySetException(ex);
            throw;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }

    protected virtual async Task WaitReadyInitQueueAsync()
    {
        foreach (var consumerType in _consumerTypes)
        {
            var routingProvider = _serviceProvider.GetRequiredService<IRoutingProvider>();
            var consumerOptions = routingProvider.Get(consumerType.ConsumerOptions);

            await InitQueueAsync(_connectionObject.DefaultChannel, consumerOptions);

            if (consumerType.Consumer.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEmptyConsumer<>)))
            {
                continue;
            }

            var currentChannel = await _connectionObject.Connection.CreateChannelAsync();
            (string consumerTag, MessageConsumer consumer) = await CreateMessageConsumer(currentChannel, consumerType.Event, consumerOptions);
            _consumers.Add(consumer, currentChannel);
        }
    }
}