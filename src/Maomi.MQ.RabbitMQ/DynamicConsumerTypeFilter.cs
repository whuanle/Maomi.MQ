// <copyright file="DynamicConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Dynamic consumer type filter.<br />
/// 消费者类型过滤器.
/// </summary>
public class DynamicConsumerTypeFilter : ITypeFilter
{
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    private readonly List<(Type, IConsumerOptions)> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicConsumerTypeFilter"/> class.
    /// </summary>
    public DynamicConsumerTypeFilter()
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
    }

    /// <inheritdoc/>
    public void Build(IServiceCollection services)
    {
        List<ConsumerType> consumerTypes = new();
        foreach (var item in _consumers)
        {
            var (type, consumerOptions) = item;

            var queueName = consumerOptions.Queue;

            var consumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>))!;

            // Each IConsumer<T> corresponds to one queue and one ConsumerHostSrvice<T>.
            // 每个 IConsumer<T> 对应一个队列、一个 ConsumerHostSrvice<T>.
            services.AddKeyedSingleton(serviceKey: queueName, serviceType: typeof(IConsumerOptions), implementationInstance: consumerOptions);
            services.Add(new ServiceDescriptor(serviceKey: consumerOptions.Queue, serviceType: consumerInterface, implementationType: type, lifetime: ServiceLifetime.Scoped));

            var eventType = consumerInterface.GenericTypeArguments[0];
            var consumerType = new ConsumerType
            {
                Queue = queueName,
                Consumer = type,
                Event = eventType
            };

            consumerTypes.Add(consumerType);
        }

        Func<IServiceProvider, object?, ConsumerBaseHostService> funcFactory = (serviceProvider, serviceKey) =>
        {
            return new ConsumerBaseHostService(
                serviceProvider,
                serviceProvider.GetRequiredService<ServiceFactory>(),
                serviceProvider.GetRequiredService<ConnectionPool>(),
                serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                consumerTypes);
        };

        services.TryAddEnumerable(new ServiceDescriptor(serviceKey: "dynamicconsumer", serviceType: typeof(IHostedService), factory: funcFactory, lifetime: ServiceLifetime.Singleton));
    }

    /// <summary>
    /// Add a custom consumer type.<br />
    /// 添加自定义消费者.
    /// </summary>
    /// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
    /// <param name="consumerOptions"></param>
    public void AddConsumer<TConsumer>(IConsumerOptions consumerOptions)
        where TConsumer : class
    {
        AddConsumer(typeof(TConsumer), consumerOptions);
    }

    /// <summary>
    /// Add a custom consumer type.<br />
    /// 添加自定义消费者.
    /// </summary>
    /// <param name="consumerType"></param>
    /// <param name="consumerOptions"></param>
    public void AddConsumer(Type consumerType, IConsumerOptions consumerOptions)
    {
        var consumerInterface = consumerType.GetInterfaces().Where(x => x.IsGenericType)
            .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerInterface == null)
        {
            throw new NotImplementedException($"{consumerType.Name} is not a valid consumer type");
        }

        _consumers.Add((consumerType, consumerOptions));
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        return;
    }
}
