// <copyright file="ConsumerHostedService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    protected readonly IHostApplicationLifetime _hostApplicationLifetime;

    protected readonly IConnectionObject _connectionObject;

    protected readonly IRetryPolicyFactory _policyFactory;

    protected readonly IReadOnlyList<ConsumerType> _consumerTypes;
    protected readonly Dictionary<string, IChannel> _consumers = new();
    private bool _basicReturnHandlerRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerHostedService"/> class.
    /// </summary>
    /// <param name="hostApplicationLifetime"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="connectionPool"></param>
    /// <param name="consumerTypes"></param>
    public ConsumerHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        ServiceFactory serviceFactory,
        ConnectionPool connectionPool,
        IReadOnlyList<ConsumerType> consumerTypes)
        : base(serviceFactory, serviceFactory.ServiceProvider.GetRequiredService<ILoggerFactory>())
    {
        _hostApplicationLifetime = hostApplicationLifetime;

        _connectionObject = connectionPool.Get();

        _policyFactory = serviceFactory.RetryPolicyFactory;
        _consumerTypes = consumerTypes;
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanupConsumersAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var applicationStartedTask = WaitForHostStartedAsync(stoppingToken);
        await applicationStartedTask;

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        _logger.LogWarning("Detects queue information and initializes it.");

        try
        {
            if (!_basicReturnHandlerRegistered)
            {
                _connectionObject.DefaultChannel.BasicReturnAsync += BasicReturnAsync;
                _basicReturnHandlerRegistered = true;
            }

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

        await CleanupConsumersAsync(stoppingToken);
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
        var idGen = _serviceProvider.GetRequiredService<IIdProvider>();

        foreach (var consumerType in _consumerTypes)
        {
            var routingProvider = _serviceProvider.GetRequiredService<IRoutingProvider>();
            var consumerOptions = routingProvider.Get(consumerType.ConsumerOptions);

            if (consumerOptions.IsBroadcast == true)
            {
                // 复制属性并修改 Queue 名称，避免广播消费者之间互相干扰
                var newConsumerOptions = new ConsumerOptions();
                newConsumerOptions.CopyFrom(consumerOptions);
                newConsumerOptions.Queue = consumerOptions.Queue + "_" + idGen.NextId();
                consumerOptions = newConsumerOptions;
            }

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

    // 避免阻塞 Web 启动，在 ASP.NET Core 启动完毕后才会启动此后台服务
    private Task<bool> WaitForHostStartedAsync(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        _hostApplicationLifetime.ApplicationStarted.Register(() =>
        {
            tcs.TrySetResult(true);
        });

        cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
        });

        return tcs.Task;
    }

    private async Task CleanupConsumersAsync(CancellationToken cancellationToken)
    {
        if (_basicReturnHandlerRegistered)
        {
            _connectionObject.DefaultChannel.BasicReturnAsync -= BasicReturnAsync;
            _basicReturnHandlerRegistered = false;
        }

        if (_consumers.Count == 0)
        {
            return;
        }

        foreach (var item in _consumers.ToArray())
        {
            try
            {
                await item.Value.BasicCancelAsync(item.Key, true, cancellationToken);
            }
            catch
            {
            }
            finally
            {
                try
                {
                    item.Value.Dispose();
                }
                catch
                {
                }

                _consumers.Remove(item.Key);
            }
        }
    }
}
