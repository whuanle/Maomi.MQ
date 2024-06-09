// <copyright file="MaomiExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Pool;
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
    /// <param name="assemblies">The assembly to be scanned.<br />需要扫描的程序集.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQ(
        this IServiceCollection services,
        Action<MqOptionsBuilder> mqOptionsBuilder,
        Assembly[] assemblies)
    {
        ITypeFilter[] typeFilters =
        [
            new ConsumerTypeFilter(),
            new EventBusTypeFilter()
        ];

        return AddMaomiMQ(services, mqOptionsBuilder, assemblies, typeFilters);
    }

    /// <summary>
    /// Use the Maomi.MQ service.<br />
    /// 注入 Maomi.MQ 服务.
    /// </summary>
    /// <param name="services">services.</param>
    /// <param name="builder">Global MQ configuration.<br />全局 MQ 配置.</param>
    /// <param name="assemblies">The assembly to be scanned.<br />需要扫描的程序集.</param>
    /// <param name="typeFilters"></param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQ(
        this IServiceCollection services,
        Action<MqOptionsBuilder> builder,
        Assembly[] assemblies,
        ITypeFilter[] typeFilters)
    {
        ArgumentNullException.ThrowIfNull(builder);

        MqOptionsBuilder optionsBuilder = new();
        ConnectionFactory connectionFactory = new ConnectionFactory();

        builder.Invoke(optionsBuilder);
        ArgumentNullException.ThrowIfNull(optionsBuilder.ConnectionFactory);
        optionsBuilder.ConnectionFactory.Invoke(connectionFactory);

        services.AddSingleton<MqOptions>(new MqOptions
        {
            WorkId = optionsBuilder.WorkId,
            AutoQueueDeclare = optionsBuilder.AutoQueueDeclare,
            ConnectionFactory = connectionFactory
        });

        services.AddMaomiMQCore();
        services.AddSingleton<IIdFactory>(new DefaultIdFactory((ushort)optionsBuilder.WorkId));
        services.AddSingleton<ServiceFactory>();

        services.AddSingleton(connectionFactory);
        services.AddSingleton<ConnectionPooledObjectPolicy>();
        services.AddSingleton<ConnectionPool>();
        services.AddSingleton<IMessagePublisher, DefaultMessagePublisher>();

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
