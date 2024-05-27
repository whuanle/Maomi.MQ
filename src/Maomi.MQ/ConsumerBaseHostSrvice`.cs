// <copyright file="ConsumerBaseHostSrvice`.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1649
#pragma warning disable SA1401
#pragma warning disable SA1600

using Maomi.MQ.Defaults;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Base consumer service.
/// </summary>
/// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
/// <typeparam name="TEvent">Event model.</typeparam>
public abstract class ConsumerBaseHostSrvice<TConsumer, TEvent> : ConsumerBaseHostSrvice
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
{
    protected readonly ConsumerOptions _consumerOptions;
    protected readonly Type _consumerType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerBaseHostSrvice{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="waitReadyFactory"></param>
    public ConsumerBaseHostSrvice(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<ConsumerBaseHostSrvice> logger,
        IRetryPolicyFactory policyFactory,
        ConsumerOptions consumerOptions,
        IWaitReadyFactory waitReadyFactory)
        : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory)
    {
        _consumerOptions = consumerOptions;
        _consumerType = typeof(TConsumer);
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
    }

    /// <inheritdoc />.
    protected override async Task WaitReadyAsync()
    {
        var consumerAttribute = typeof(TConsumer).GetCustomAttribute<ConsumerAttribute>()!;

        if (_connectionOptions.AutoQueueDeclare)
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    Dictionary<string, object> arguments = new();
                    if (!string.IsNullOrEmpty(consumerAttribute.DeadQueue))
                    {
                        arguments.Add("x-dead-letter-exchange", consumerAttribute.DeadQueue);
                    }

                    // Create queues based on consumers.
                    // 根据消费者创建队列.
                    await channel.QueueDeclareAsync(
                        queue: _consumerOptions.Queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: arguments);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        using IConnection connection = await _connectionFactory.CreateConnectionAsync();
        using IChannel channel = await connection.CreateChannelAsync();
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _consumerOptions.Qos, global: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, _consumerOptions.Queue } };
            if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
            {
                loggerState.Add(DiagnosticName.Event.Id, eventId!);
            }

            using (_logger.BeginScope(loggerState))
            {
                await ConsumerAsync<TEvent>(channel, _consumerOptions, eventArgs);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _consumerOptions.Queue,
            autoAck: false,
            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }
}
