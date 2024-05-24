// <copyright file="ConsumerBaseHostSrvice.cs" company="Maomi">
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

namespace Maomi.MQ;

/// <summary>
/// Base consumer service.
/// </summary>
/// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
/// <typeparam name="TEvent">Event model.</typeparam>
public abstract class ConsumerBaseHostSrvice<TConsumer, TEvent> : BackgroundService
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
{
    /// <summary>
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    protected readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// <see cref="DefaultMqOptions"/>.
    /// </summary>
    protected readonly DefaultMqOptions _connectionOptions;

    /// <summary>
    /// <see cref="ConnectionFactory"/>.
    /// </summary>
    protected readonly IConnectionFactory _connectionFactory;

    /// <summary>
    /// Type.
    /// </summary>
    protected readonly Type _consumerType;

    /// <summary>
    /// <see cref="ConsumerOptions"/>.
    /// </summary>
    protected readonly ConsumerOptions _consumerOptions;

    /// <summary>
    /// <see cref="IJsonSerializer"/>.
    /// </summary>
    protected readonly IJsonSerializer _jsonSerializer;

    /// <summary>
    /// <see cref="IRetryPolicyFactory"/>.
    /// </summary>
    private readonly IRetryPolicyFactory _policyFactory;

    /// <summary>
    /// <see cref="IWaitReadyFactory"/>.
    /// </summary>
    private readonly IWaitReadyFactory _waitReadyFactory;

    /// <summary>
    /// logger.
    /// </summary>
    protected readonly ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> _logger;

    private readonly TaskCompletionSource _taskCompletionSource;

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
        ILogger<MQ.ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
        IRetryPolicyFactory policyFactory,
        ConsumerOptions consumerOptions,
        IWaitReadyFactory waitReadyFactory)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _consumerType = typeof(TConsumer);
        _connectionOptions = connectionOptions;
        _connectionFactory = connectionOptions.ConnectionFactory;

        _consumerOptions = consumerOptions;

        _policyFactory = policyFactory;
        _waitReadyFactory = waitReadyFactory;
        _taskCompletionSource = new();
        _waitReadyFactory.AddTask(_taskCompletionSource.Task);
    }

    /// <inheritdoc />.
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (IConnection connection = await _connectionFactory.CreateConnectionAsync())
            {
                using (IChannel channel = await connection.CreateChannelAsync())
                {
                    // Create queues based on consumers.
                    // 根据消费者创建队列.
                    await channel.QueueDeclareAsync(
                        queue: _consumerOptions.Queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);
                }
            }

            _taskCompletionSource.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while declaring the queue,queue [{Queue}].", _consumerOptions.Queue);
            _taskCompletionSource?.TrySetException(ex);
            throw;
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
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _consumerOptions.Qos, global: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (sender, eventArgs) =>
        {
            await ConsumerAsync(channel, eventArgs);
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

    /// <summary>
    /// Consumer messsage.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="eventArgs"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual async Task ConsumerAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
    {
        var scope = _serviceProvider.CreateScope();
        var ioc = scope.ServiceProvider;

        var consumer = ioc.GetRequiredService<IConsumer<TEvent>>();
        EventBody<TEvent>? eventBody = null;

        try
        {
            eventBody = _jsonSerializer.Deserialize<EventBody<TEvent>>(eventArgs.Body.Span)!;

            // Executed on the last retry.
            // 最后一次重试失败时执行.
            var fallbackPolicy = Policy<bool>
                .Handle<Exception>()
                .FallbackAsync(async (c) =>
                {
                    try
                    {
                        return await consumer.FallbackAsync(eventBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "An error occurred while executing the FallbackAsync method,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody.Id);
                        return false;
                    }
                });

            // Each retry.
            // 每次失败时执行.
            int retryCount = 0;
            var retryEachPolicy = Policy.Handle<Exception>().RetryAsync(async (ex, count) =>
            {
                try
                {
                    retryCount++;
                    await consumer.FaildAsync(ex, retryCount, eventBody);
                }
                catch (Exception faildEx)
                {
                    _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody.Id);
                }
            });

            // Custom retry policy.
            // 自定义重试策略.
            var customRetryPolicy = await _policyFactory.CreatePolicy(_consumerOptions.Queue);

            var policyWrap = fallbackPolicy.WrapAsync(customRetryPolicy).WrapAsync(retryEachPolicy);

            var executeResult = await policyWrap.ExecuteAsync(async () =>
            {
                await consumer.ExecuteAsync(eventBody);
                return true;
            });

            // The execution completed normally, or the FallbackAsync function was executed to compensate for the last retry.
            // 正常执行完成，或执行了 FallbackAsync 函数补偿最后一次重试.
            if (executeResult)
            {
                await channel.BasicAckAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false);
            }
            else
            {
                // Whether to put it back to the queue when the last retry fails.
                // 最后一次重试失败时，是否放回队列.
                await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.RetryFaildRequeue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while processing the message,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody?.Id);

            try
            {
                await consumer.FaildAsync(ex, -1, eventBody);
            }
            catch (Exception faildEx)
            {
                _logger.LogWarning(faildEx, "An error occurred while executing the FaildAsync method,queue [{Queue}],id [{Id}].", _consumerOptions.Queue, eventBody?.Id);
            }

            await channel.BasicNackAsync(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: _consumerOptions.ExecptionRequeue);
        }
    }
}
