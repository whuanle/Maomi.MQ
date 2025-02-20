// <copyright file="ConsumerHostedService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.Initialize the queue and build the consumer application.<br />
/// 初始化队列和构建消费者程序.
/// </summary>
public partial class ConsumerHostedService : ConsumerBaseService
{
    protected readonly IConnectionObject _connectionObject;

    protected readonly IMessageSerializer _jsonSerializer;
    protected readonly IRetryPolicyFactory _policyFactory;

    protected readonly IReadOnlyList<ConsumerType> _consumerTypes;
    protected readonly Dictionary<string, IChannel> _consumers = new();

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
        : base(serviceFactory, serviceFactory.ServiceProvider.GetRequiredService<ILoggerFactory>())
    {
        _connectionObject = connectionPool.Get();

        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _consumerTypes = consumerTypes;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("Detects queue information and initializes it.");

        try
        {
            _connectionObject.DefaultChannel.BasicReturnAsync += BasicReturnAsync;
            await WaitReadyInitQueueAsync();
            _logger.LogWarning("The consumer host has been started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue.Consumer services have been withdrawn.");
            throw;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }

    protected virtual async Task BasicReturnAsync(object sender, BasicReturnEventArgs @event)
    {
        using var scope = _serviceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var breakdown = serviceProvider.GetRequiredService<IBreakdown>();
        await breakdown.BasicReturnAsync(sender, @event);
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
            var createConsumer = BuildCreateConsumerHandler(consumerType.Event);
            var consumerTag = await createConsumer(this, currentChannel, consumerType.Consumer, consumerType.Event, consumerOptions);
            _consumers.Add(consumerTag, currentChannel);
        }
    }
}