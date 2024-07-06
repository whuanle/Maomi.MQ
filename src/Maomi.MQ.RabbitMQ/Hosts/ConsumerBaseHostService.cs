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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq.Expressions;
using System.Reflection;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.Initialize the queue and build the consumer application.<br />
/// 初始化队列和构建消费者程序.
/// </summary>
public partial class ConsumerBaseHostService : BackgroundService
{
    protected readonly TaskCompletionSource _taskCompletionSource;
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly IServiceProvider _serviceProvider;
    protected readonly ServiceFactory _serviceFactory;
    protected readonly MqOptions _mqOptions;
    protected readonly IConnectionFactory _connectionFactory;
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
    /// <param name="logger"></param>
    /// <param name="consumerTypes"></param>
    public ConsumerBaseHostService(
        IServiceProvider serviceProvider,
        ServiceFactory serviceFactory,
        ILogger<ConsumerBaseHostService> logger,
        IReadOnlyList<ConsumerType> consumerTypes)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _mqOptions = serviceFactory.Options;
        _connectionFactory = serviceFactory.ConnectionFactory;

        _serviceFactory = serviceFactory;
        _jsonSerializer = serviceFactory.Serializer;
        _policyFactory = serviceFactory.RetryPolicyFactory;
        _waitReadyFactory = serviceFactory.WaitReadyFactory;
        _taskCompletionSource = new();
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
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
        using IConnection connection = await _connectionFactory.CreateConnectionAsync();

        try
        {
            await WaitReadyInitQueueAsync(connection);
            _taskCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue.");
            _taskCompletionSource?.TrySetException(ex);
            throw;
        }

        int consumerCount = await WaitReadyConsumerAsync(connection);

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
            Dictionary<string, object> arguments = new();
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: consumerType.Queue);

            if (consumerOptions.AutoQueueDeclare == AutoQueueDeclare.Disable)
            {
                continue;
            }
            else if (!_mqOptions.AutoQueueDeclare && consumerOptions.AutoQueueDeclare == AutoQueueDeclare.None)
            {
                continue;
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
            await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
            var consumer = new EventingBasicConsumer(consummerChannel);

            var consumerHandler = BuildConsumerHandler(consumerType.Event);
            MessageConsumer messageConsumer = new MessageConsumer(_serviceProvider, _serviceFactory, _serviceProvider.GetRequiredService<ILogger<MessageConsumer>>(), consumerOptions);
            _consumers.Add(consumerOptions.Queue, messageConsumer);

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
        }

        return consumerCount;
    }
}

/// <summary>
/// Base consumer service.
/// </summary>
public partial class ConsumerBaseHostService
{
    /// <summary>
    /// Consumer method.
    /// </summary>
    protected static readonly MethodInfo ConsumerMethod = typeof(MessageConsumer)
        .GetMethod(nameof(MessageConsumer.ConsumerAsync), BindingFlags.Instance | BindingFlags.Public)!;

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
