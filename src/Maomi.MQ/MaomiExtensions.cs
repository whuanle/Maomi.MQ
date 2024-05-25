// <copyright file="MaomiExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using IdGen;
using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Extensions;
using Maomi.MQ.Pool;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Extension.
/// </summary>
public static partial class MaomiExtensions
{
    /// <summary>
    /// Use the Maomi.MQ service.<br />
    /// 注入 Maomi.MQ 服务.
    /// </summary>
    /// <param name="services">services.</param>
    /// <param name="mqOptionsBuilder">Global MQ configuration.<br />全局 MQ 配置.</param>
    /// <param name="factoryBuilder">RabbitMQ connection configuration<br/>RabbitMQ 连接配置.</param>
    /// <param name="assemblies">The assembly to be scanned.<br />需要扫描的程序集.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQ(
        this IServiceCollection services,
        Action<MqOptions> mqOptionsBuilder,
        Action<ConnectionFactory> factoryBuilder,
        Assembly[] assemblies)
    {
        ITypeFilter[] typeFilters =
        [
            new ConsumerTypeFilter(),
            new EventBusTypeFilter()
        ];

        return AddMaomiMQ(services, mqOptionsBuilder, factoryBuilder, assemblies, typeFilters);
    }

    /// <summary>
    /// Use the Maomi.MQ service.<br />
    /// 注入 Maomi.MQ 服务.
    /// </summary>
    /// <param name="services">services.</param>
    /// <param name="mqOptionsBuilder">Global MQ configuration.<br />全局 MQ 配置.</param>
    /// <param name="factoryBuilder">RabbitMQ connection configuration<br/>RabbitMQ 连接配置.</param>
    /// <param name="assemblies">The assembly to be scanned.<br />需要扫描的程序集.</param>
    /// <param name="typeFilters"></param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQ(
        this IServiceCollection services,
        Action<MqOptions> mqOptionsBuilder,
        Action<ConnectionFactory> factoryBuilder,
        Assembly[] assemblies,
        ITypeFilter[] typeFilters)
    {
        ConnectionFactory connectionFactory = new ConnectionFactory();

        if (factoryBuilder != null)
        {
            factoryBuilder.Invoke(connectionFactory);
        }

        var connectionOptions = new DefaultMqOptions
        {
            ConnectionFactory = connectionFactory
        };

        if (mqOptionsBuilder != null)
        {
            mqOptionsBuilder.Invoke(connectionOptions);
        }

        services.AddIdGen(connectionOptions.WorkId, () => IdGeneratorOptions.Default);

        services.AddSingleton<MqOptions>(connectionOptions);
        services.AddSingleton(connectionOptions);
        services.AddSingleton<ConnectionPooledObjectPolicy>();
        services.AddSingleton<ConnectionPool>();

        services.AddSingleton<IJsonSerializer, DefaultJsonSerializer>();

        services.AddSingleton<IMessagePublisher, DefaultMessagePublisher>();

        services.AddSingleton<IWaitReadyFactory, DefaultWaitReadyFactory>();
        services.AddSingleton<IRetryPolicyFactory, DefaultRetryPolicyFactory>();
        services.AddSingleton<ICircuitBreakerFactory, DefaultCircuitBreakerFactory>();

        List<Type> types = new();

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var item in typeFilters)
                {
                    item.Filter(services, type);
                }
            }
        }

        foreach (var item in typeFilters)
        {
            item.Build(services);
        }

        services.AddHostedService<WaitReadyHostService>();
        return services;
    }
}
