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
/// Consumer type filter.<br />
/// 消费者类型过滤器.
/// </summary>
public class ConsumerTypeFilter : ITypeFilter
{
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    private readonly Dictionary<string, List<ConsumerType>> _consumerGroups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerTypeFilter"/> class.
    /// </summary>
    public ConsumerTypeFilter()
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
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

        if (!string.IsNullOrEmpty(consumerAttribute.Group))
        {
            if (!_consumerGroups.TryGetValue(consumerAttribute.Group, out var list))
            {
                list = new();
                _consumerGroups[consumerAttribute.Queue] = list;
            }

            list.Add(consumerType);

            return;
        }

        var hostType = typeof(ConsumerHostService<,>).MakeGenericType(type, eventType);
        AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
    }
}
