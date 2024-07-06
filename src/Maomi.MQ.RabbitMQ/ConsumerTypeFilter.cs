// <copyright file="ConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly Dictionary<string, List<ConsumerType>> _consumerGroups = new();

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
        foreach (var item in _consumerGroups)
        {
            services.AddHostedService(s =>
            {
                return new ConsumerBaseHostService(s, s.GetRequiredService<ServiceFactory>(), s.GetRequiredService<ILogger<ConsumerBaseHostService>>(), item.Value);
            });
        }
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        if (!type.IsClass)
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
            throw new ArgumentNullException($"{type.Name} type is not configured with the [Consumer] attribute.");
        }

        var queueName = consumerAttribute.Queue;
        if (_consumerInterceptor != null)
        {
            var isRegister = _consumerInterceptor.Invoke(consumerAttribute, type);
            if (!isRegister)
            {
                return;
            }
        }

        // Each IConsumer<T> corresponds to one queue and one ConsumerHostSrvice<T>.
        // 每个 IConsumer<T> 对应一个队列、一个 ConsumerHostSrvice<T>.
        services.AddKeyedSingleton(serviceKey: queueName, serviceType: typeof(IConsumerOptions), implementationInstance: consumerAttribute);
        services.Add(new ServiceDescriptor(serviceKey: consumerAttribute.Queue, serviceType: consumerInterface, implementationType: type, lifetime: ServiceLifetime.Scoped));

        var eventType = consumerInterface.GenericTypeArguments[0];
        var consumerType = new ConsumerType
        {
            Queue = queueName,
            Consumer = type,
            Event = eventType
        };

        if (!string.IsNullOrEmpty(consumerAttribute.Group))
        {
            if (!_consumerGroups.TryGetValue(consumerAttribute.Group, out var list))
            {
                list = new();
                _consumerGroups[consumerAttribute.Group] = list;
            }

            list.Add(consumerType);

            return;
        }

        var hostType = typeof(ConsumerHostService<,>).MakeGenericType(type, eventType);
        AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
    }
}
