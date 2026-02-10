// <copyright file="FastEndpointsTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;
using Maomi.MQ.Consumer;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// FastEndpointsEventBusTypeFilter.
/// </summary>
public class FastEndpointsTypeFilter : ITypeFilter
{
    private readonly ConsumerInterceptor? _consumerInterceptor;
    private readonly HashSet<ConsumerType> _consumers = new();
    private readonly Type _eventMiddleware;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Filter.</param>
    /// <param name="eventMiddleware"></param>
    public FastEndpointsTypeFilter(ConsumerInterceptor? consumerInterceptor = null, Type? eventMiddleware = null)
    {
        _consumerInterceptor = consumerInterceptor;
        if (eventMiddleware != null)
        {
            if (!eventMiddleware.IsGenericType || !eventMiddleware.GetGenericTypeDefinition().IsAssignableTo(typeof(IEventMiddleware<>)))
            {
                throw new TypeLoadException($"The type {eventMiddleware.Name} is not a valid event middleware. Example: public class DefaultFastEndpointsEventMiddleware<TCommand> : IEventMiddleware<TCommand>> {"{}"}");
            }
        }
        else
        {
            eventMiddleware = typeof(DefaultFastEndpointsEventMiddleware<>);
        }

        _eventMiddleware = eventMiddleware;
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

        // FastEndpoints 有 三种模式
        // normal | : IEvent | ICommand<TResponse>
        if (!type.IsAssignableTo(typeof(IEvent)) && !type.IsAssignableTo(typeof(ICommand)))
        {
            return;
        }

        IConsumerOptions? consumerAttribute = type.GetCustomAttribute<FastEndpointsCommandAttribute>() ?? type.GetCustomAttribute<FCommandAttribute>();

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

        var consumerInterface = typeof(IConsumer<>).MakeGenericType(type);
        var implementationType = typeof(FastEndpointsConsumer<>).MakeGenericType(type);

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: consumerInterface, implementationType: implementationType, lifetime: ServiceLifetime.Scoped));
        services.Add(new ServiceDescriptor(serviceType: implementationType, implementationType: implementationType, lifetime: ServiceLifetime.Scoped));

        // Add IEventMiddleware<T>
        services.Add(new ServiceDescriptor(serviceType: typeof(IEventMiddleware<>).MakeGenericType(type), implementationType: _eventMiddleware.MakeGenericType(type), lifetime: ServiceLifetime.Scoped));

        var eventType = type;
        var consumerType = new ConsumerType
        {
            Queue = consumerAttribute.Queue,
            Consumer = implementationType,
            Event = eventType,
            ConsumerOptions = consumerAttribute
        };

        _consumers.Add(consumerType);
    }
}
