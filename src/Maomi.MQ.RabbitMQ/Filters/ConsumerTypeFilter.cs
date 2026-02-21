// <copyright file="ConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Add the consumers with the [<see cref="ConsumerAttribute"/>] feature to the container.<br />
/// 将带有 [<see cref="ConsumerAttribute"/>] 特性的消费者添加到容器中.
/// </summary>
public class ConsumerTypeFilter : ITypeFilter
{
    private readonly HashSet<ConsumerType> _consumers = new();

    private readonly ConsumerInterceptor? _consumerInterceptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Filter.</param>
    public ConsumerTypeFilter(ConsumerInterceptor? consumerInterceptor = null)
    {
        _consumerInterceptor = consumerInterceptor;
    }

    /// <inheritdoc/>
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        return _consumers.ToList();
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

        IConsumerOptions? consumerAttribute = type.GetCustomAttribute<ConsumerAttribute>();

        if (consumerAttribute == null || string.IsNullOrEmpty(consumerAttribute.Queue))
        {
            return;
        }

        if (_consumerInterceptor != null)
        {
            var register = _consumerInterceptor.Invoke(consumerAttribute, type);
            if (!register.IsRegister)
            {
                return;
            }

            consumerAttribute = register.ConsumerOptions.Clone();
        }

        if (_consumers.FirstOrDefault(x => x.Queue == consumerAttribute.Queue) is ConsumerType existConsumerType)
        {
            if (existConsumerType.ConsumerOptions.IsBroadcast != true)
            {
                throw new ArgumentException($"Multiple consumers are bound to the same queue. queue: [{consumerAttribute.Queue}],consumer: {existConsumerType.Consumer.Name} and {type.Name}");
            }
        }

        services.Add(new ServiceDescriptor(serviceType: type, implementationType: type, lifetime: ServiceLifetime.Scoped));

        var eventType = consumerInterface.GenericTypeArguments[0];
        var consumerType = new ConsumerType
        {
            Queue = consumerAttribute.Queue,
            Consumer = type,
            Event = eventType,
            ConsumerOptions = consumerAttribute
        };

        _consumers.Add(consumerType);
    }
}