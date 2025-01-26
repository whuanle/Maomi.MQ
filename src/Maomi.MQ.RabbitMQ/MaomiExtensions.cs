// <copyright file="MaomiExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
        ArgumentNullException.ThrowIfNull(optionsBuilder.Rabbit);
        optionsBuilder.Rabbit.Invoke(connectionFactory);

        services.AddSingleton<MqOptions>(new MqOptions
        {
            AppName = optionsBuilder.AppName,
            WorkId = optionsBuilder.WorkId,
            AutoQueueDeclare = optionsBuilder.AutoQueueDeclare,
            ConnectionFactory = connectionFactory
        });

        services.AddMaomiMQCore();
        services.AddScoped<IBreakdown, DefaultBreakdown>();
        services.AddSingleton<IRoutingProvider, RoutingProvider>();
        services.AddSingleton<IIdFactory>(new DefaultIdFactory((ushort)optionsBuilder.WorkId));
        services.AddSingleton<ServiceFactory>();
        services.AddSingleton<IDynamicConsumer, DynamicConsumerService>();

        services.AddSingleton<ConnectionPool>();

        services.AddScoped<IMessagePublisher, DefaultMessagePublisher>();

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

        List<ConsumerType> consumerTypes = new();

        foreach (var filter in typeFilters)
        {
            var types = filter.Build(services);
            consumerTypes.AddRange(types);
        }

        services.AddSingleton<IConsumerTypeProvider>(new ConsumerTypeProvider(consumerTypes));

        Func<IServiceProvider, ConsumerHostedService> funcFactory = (serviceProvider) =>
        {
            return new ConsumerHostedService(
                serviceProvider.GetRequiredService<ServiceFactory>(),
                serviceProvider.GetRequiredService<ConnectionPool>(),
                consumerTypes);
        };

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory, lifetime: ServiceLifetime.Singleton));

        return services;
    }
}
