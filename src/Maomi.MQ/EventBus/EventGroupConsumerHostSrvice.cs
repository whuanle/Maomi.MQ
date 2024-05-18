// <copyright file="EventGroupConsumerHostSrvice.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Queues in the same group are executed on a channel.<br />
/// 将同一分组下的队列放到一个通道下执行.
/// </summary>
public class EventGroupConsumerHostSrvice : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DefaultMqOptions _connectionOptions;
    private readonly ConnectionFactory _connectionFactory;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IPolicyFactory _policyFactory;
    private readonly ILogger<EventGroupConsumerHostSrvice> _logger;
    private readonly EventGroupInfo _eventGroupInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGroupConsumerHostSrvice"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="eventGroupInfo"></param>
    public EventGroupConsumerHostSrvice(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<EventGroupConsumerHostSrvice> logger,
        IPolicyFactory policyFactory,
        EventGroupInfo eventGroupInfo)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connectionOptions = connectionOptions;
        _connectionFactory = connectionOptions.ConnectionFactory;
        _policyFactory = policyFactory;
        _eventGroupInfo = eventGroupInfo;
    }

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
        {
            using (IChannel channel = await connection.CreateChannelAsync())
            {
                foreach (var item in _eventGroupInfo.EventInfos)
                {
                    await channel.QueueDeclareAsync(
                        queue: item.Value.Queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                }
            }
        }

        await base.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        using IConnection connection = await _connectionFactory.CreateConnectionAsync();
        using IChannel channel = await connection.CreateChannelAsync();

        var qos = (ushort)_eventGroupInfo.EventInfos.Average(x => x.Value.Qos);
        if (qos <= 0)
        {
            qos = 1;
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: qos, global: false);

        foreach (var item in _eventGroupInfo.EventInfos)
        {
            // 定义消费者
            var consumer = new EventingBasicConsumer(channel);

            var method = ConsumerMethod.MakeGenericMethod(item.Value.EventType);
            consumer.Received += async (sender, eventArgs) =>
            {
                await (Task)method.Invoke(this, new object[] { item.Value, channel, sender, eventArgs });
            };

            await channel.BasicConsumeAsync(
                queue: item.Value.Queue,
                autoAck: false,
                consumer: consumer);
        }

        while (true)
        {
            await Task.Delay(1000);
        }
    }

    private static readonly MethodInfo ConsumerMethod = typeof(EventGroupConsumerHostSrvice).GetMethod("ConsumerAsync", BindingFlags.Instance | BindingFlags.NonPublic);
    
    protected virtual async Task ConsumerAsync<TEvent>(EventInfo eventInfo, IChannel channel, object? sender, BasicDeliverEventArgs eventArgs)
    where TEvent : class
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        var consumer = ioc.GetRequiredService<IConsumer<TEvent>>();

        EventBody<TEvent> eventBody = null!;

        try
        {
            eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;

            var retryPolicy = await _policyFactory.CreatePolicy(eventInfo.Queue);

            var fallbackPolicy = Policy
                .Handle<Exception>()
                .FallbackAsync(async (c) =>
                {
                    await consumer.FallbackAsync(eventBody);
                });

            var retryAnyPolicy = Policy.Handle<Exception>().RetryAsync(async (ex, count) =>
            {
                await consumer.FaildAsync(eventBody);
            });

            var policyWrap = Policy.WrapAsync(fallbackPolicy, retryAnyPolicy, retryPolicy);

            await policyWrap.ExecuteAsync(async () =>
            {
                await consumer.ExecuteAsync(eventBody);
            });

            await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            try
            {
                await consumer.FaildAsync(eventBody);
            }
            catch (Exception ex1)
            {

            }

            if (eventInfo.Qos == 1)
            {
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
            else
            {
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: eventInfo.Requeue);
            }
            throw;
        }
    }
}
