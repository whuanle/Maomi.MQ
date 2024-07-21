// <copyright file="ConsumerTypeFilter.cs" company="Maomi">
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
/// <see cref="ConsumerInterceptor"/> filter.
/// </summary>
/// <remarks>You can modify related parameters.<br />可以修改相关参数.</remarks>
/// <param name="consumerAttribute"></param>
/// <param name="consumerType"></param>
/// <returns>Whether to register the event.<br />是否注册该事件.</returns>
public delegate bool ConsumerInterceptor(ConsumerAttribute consumerAttribute, Type consumerType);

/// <summary>
/// Consumer type filter.<br />
/// 消费者类型过滤器.
/// </summary>
public class ConsumerTypeFilter : ITypeFilter
{
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    private readonly HashSet<ConsumerType> _consumers = new();

    private readonly ConsumerInterceptor? _consumerInterceptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Filter.</param>
    public ConsumerTypeFilter(ConsumerInterceptor? consumerInterceptor = null)
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
        _consumerInterceptor = consumerInterceptor;
    }

    /// <inheritdoc/>
    public void Build(IServiceCollection services)
    {
        Func<IServiceProvider, ConsumerHostService> funcFactory = (serviceProvider) =>
        {
            return new ConsumerHostService(
                serviceProvider,
                serviceProvider.GetRequiredService<ServiceFactory>(),
                serviceProvider.GetRequiredService<ConnectionPool>(),
                serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                _consumers.ToList());
        };

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory, lifetime: ServiceLifetime.Singleton));
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        if (!type.IsClass || type.IsAbstract)
        {
            return;
        }

        var consumerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
            .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IConsumer<>));

        if (consumerInterface == null)
        {
            return;
        }

        var consumerAttribute = type.GetCustomAttribute<ConsumerAttribute>();

        if (consumerAttribute == null || string.IsNullOrEmpty(consumerAttribute.Queue))
        {
            return;
        }

        if (_consumerInterceptor != null)
        {
            var isRegister = _consumerInterceptor.Invoke(consumerAttribute, type);
            if (!isRegister)
            {
                return;
            }
        }

        if (_consumers.Contains(new ConsumerType { Queue = consumerAttribute.Queue }))
        {
            return;
        }

        // Each IConsumer<T> corresponds to one queue and one ConsumerHostSrvice<T>.
        // 每个 IConsumer<T> 对应一个队列、一个 ConsumerHostSrvice<T>.
        services.AddKeyedSingleton(serviceKey: consumerAttribute.Queue, serviceType: typeof(IConsumerOptions), implementationInstance: consumerAttribute);
        services.Add(new ServiceDescriptor(serviceKey: consumerAttribute.Queue, serviceType: consumerInterface, implementationType: type, lifetime: ServiceLifetime.Scoped));

        var eventType = consumerInterface.GenericTypeArguments[0];
        var consumerType = new ConsumerType
        {
            Queue = consumerAttribute.Queue,
            Consumer = type,
            Event = eventType
        };

        _consumers.Add(consumerType);
    }
}