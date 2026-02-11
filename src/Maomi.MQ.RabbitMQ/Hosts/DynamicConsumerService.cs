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
    protected readonly IConnectionObject _connectionObject;

    private readonly ConcurrentDictionary<string, DynamicConsumerRegistration> _consumers;
    private readonly ConcurrentDictionary<string, string> _consumerTags;

    private bool _disposed;

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
        _consumerTags = new();
        _consumerTypeProvider = consumerTypeProvider;
    }

    /// <inheritdoc />
    public virtual Task<string> EventBusAsync<TMessage>(IConsumerOptions consumerOptions)
        where TMessage : class
    {
        return ConsumerAsync<EventBusConsumer<TMessage>, TMessage>(consumerOptions);
    }

    /// <inheritdoc />
    public virtual async Task<string> ConsumerAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions)
    where TMessage : class
    where TConsumer : class, IConsumer<TMessage>
    {
        EnsureQueueCanBeUsed(consumerOptions);

        var consummerChannel = _connectionObject.DefaultChannel;
        await InitQueueAsync(consummerChannel, consumerOptions);

        var createConsumer = BuildCreateConsumerHandler(typeof(TMessage));
        var consumerTag = await createConsumer(this, consummerChannel, typeof(TConsumer), typeof(TMessage), consumerOptions);

        var registerResult = TryRegisterConsumer(consumerOptions.Queue, new DynamicConsumerRegistration(consumerTag, consummerChannel, ownsChannel: false));
        if (registerResult != RegisterConsumerResult.Success)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true);
            ThrowRegisterConflict(registerResult, consumerOptions.Queue, consumerTag);
        }

        return consumerTag;
    }

    /// <inheritdoc />
    public virtual async Task<string> ConsumerAsync<TMessage>(
        IConsumerOptions consumerOptions,
        ConsumerExecuteAsync<TMessage> execute,
        ConsumerFaildAsync<TMessage>? faild = null,
        ConsumerFallbackAsync<TMessage>? fallback = null)
        where TMessage : class
    {
        EnsureQueueCanBeUsed(consumerOptions);

        var dynamicProxyConsumer = new DynamicProxyConsumer<TMessage>(execute, faild, fallback);
        var consummerChannel = await _connectionObject.Connection.CreateChannelAsync();
        await InitQueueAsync(consummerChannel, consumerOptions);

        string consumerTag = await CreateDynamicMessageConsumer<TMessage>(consummerChannel, consumerOptions, dynamicProxyConsumer);

        var registerResult = TryRegisterConsumer(consumerOptions.Queue, new DynamicConsumerRegistration(consumerTag, consummerChannel, ownsChannel: true));
        if (registerResult != RegisterConsumerResult.Success)
        {
            await consummerChannel.BasicCancelAsync(consumerTag, true);
            consummerChannel.Dispose();
            ThrowRegisterConflict(registerResult, consumerOptions.Queue, consumerTag);
        }

        return consumerTag;
    }

    /// <inheritdoc />
    public virtual async Task StopConsumerAsync(string queue)
    {
        if (_consumers.TryRemove(queue, out var registration))
        {
            _consumerTags.TryRemove(registration.ConsumerTag, out _);
            await CancelAndDisposeConsumerAsync(registration);
        }
    }

    /// <inheritdoc />
    public virtual async Task StopConsumerTagAsync(string consumerTag)
    {
        if (_consumerTags.TryRemove(consumerTag, out var queue) && _consumers.TryRemove(queue, out var registration))
        {
            await CancelAndDisposeConsumerAsync(registration);
            return;
        }

        await _connectionObject.DefaultChannel.BasicCancelAsync(consumerTag, true);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var item in _consumers.ToArray())
        {
            if (!_consumers.TryRemove(item.Key, out var registration))
            {
                continue;
            }

            _consumerTags.TryRemove(registration.ConsumerTag, out _);

            try
            {
                registration.Channel.BasicCancelAsync(registration.ConsumerTag, true).GetAwaiter().GetResult();
            }
            catch
            {
            }
            finally
            {
                if (registration.OwnsChannel)
                {
                    try
                    {
                        registration.Channel.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
        }

        _consumerTags.Clear();
        _disposed = true;

        base.Dispose();
    }

    protected virtual async Task<string> CreateDynamicMessageConsumer<TMessage>(IChannel consummerChannel, IConsumerOptions consumerOptions, DynamicProxyConsumer<TMessage> proxyConsumer)
        where TMessage : class
    {
        await consummerChannel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumerOptions.Qos, global: false);
        var consumer = new AsyncEventingBasicConsumer(consummerChannel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            try
            {
                Dictionary<string, object> loggerState = CreateLoggerState(eventArgs, consumerOptions);

                using (_logger.BeginScope(loggerState))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var serviceProvider = scope.ServiceProvider;

                    MessageConsumer<TMessage> messageConsumer = new MessageConsumer<TMessage>(serviceProvider, consumerOptions, s => proxyConsumer);
                    await messageConsumer.ConsumerAsync(consummerChannel, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while declaring the queue.Consumer services have been withdrawn.");
                throw;
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

    private void EnsureQueueCanBeUsed(IConsumerOptions consumerOptions)
    {
        var existConsumer = _consumerTypeProvider.FirstOrDefault(x => x.Queue == consumerOptions.Queue);
        if (existConsumer != null)
        {
            throw new ArgumentException($"Queue[{consumerOptions.Queue}] have been used by consumer [{existConsumer.Event.Name}]");
        }
    }

    private RegisterConsumerResult TryRegisterConsumer(string queue, DynamicConsumerRegistration registration)
    {
        if (!_consumers.TryAdd(queue, registration))
        {
            return RegisterConsumerResult.QueueConflict;
        }

        if (!_consumerTags.TryAdd(registration.ConsumerTag, queue))
        {
            _consumers.TryRemove(queue, out _);
            return RegisterConsumerResult.ConsumerTagConflict;
        }

        return RegisterConsumerResult.Success;
    }

    private void ThrowRegisterConflict(RegisterConsumerResult registerResult, string queue, string consumerTag)
    {
        if (registerResult == RegisterConsumerResult.QueueConflict)
        {
            throw new ArgumentException($"Queue[{queue}] have been used by dynamic consumer");
        }

        if (registerResult == RegisterConsumerResult.ConsumerTagConflict)
        {
            throw new ArgumentException($"ConsumerTag[{consumerTag}] have been used by dynamic consumer");
        }

        throw new InvalidOperationException("Unknown dynamic consumer register result.");
    }

    private async Task CancelAndDisposeConsumerAsync(DynamicConsumerRegistration registration)
    {
        await registration.Channel.BasicCancelAsync(registration.ConsumerTag, true);

        if (registration.OwnsChannel)
        {
            registration.Channel.Dispose();
        }
    }

    private enum RegisterConsumerResult
    {
        Success,
        QueueConflict,
        ConsumerTagConflict
    }

    private sealed class DynamicConsumerRegistration
    {
        public DynamicConsumerRegistration(string consumerTag, IChannel channel, bool ownsChannel)
        {
            ConsumerTag = consumerTag;
            Channel = channel;
            OwnsChannel = ownsChannel;
        }

        public string ConsumerTag { get; }

        public IChannel Channel { get; }

        public bool OwnsChannel { get; }
    }
}
