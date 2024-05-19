// <copyright file="ConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Microsoft.Extensions.DependencyInjection;
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
        services.Add(new ServiceDescriptor(consumerInterface, type, ServiceLifetime.Transient));

        var eventType = consumerInterface.GenericTypeArguments[0];
        var hostType = typeof(DefaultConsumerHostService<,>).MakeGenericType(type, eventType);
        AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
    }
}
