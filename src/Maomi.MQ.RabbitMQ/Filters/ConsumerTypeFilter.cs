﻿// <copyright file="ConsumerTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Consumer type filter.<br />
/// 消费者类型过滤器.
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

            consumerAttribute = register.Options.Clone();
        }

        if (_consumers.FirstOrDefault(x => x.Queue == consumerAttribute.Queue) is ConsumerType existConsumerType)
        {
            throw new ArgumentException($"Repeat bound queue [{consumerAttribute.Queue}],{existConsumerType.Event.Name} and {type.Name}");
        }

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: consumerInterface, implementationType: type, lifetime: ServiceLifetime.Scoped));
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