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
/// FastEndpoints type filter.
/// </summary>
public class FastEndpointsTypeFilter : ITypeFilter
{
    private readonly ConsumerInterceptor? _consumerInterceptor;
    private readonly HashSet<ConsumerType> _consumers = new();
    private readonly Type _eventMiddleware;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Consumer interceptor.</param>
    /// <param name="eventMiddleware">Custom middleware open generic type.</param>
    public FastEndpointsTypeFilter(ConsumerInterceptor? consumerInterceptor = null, Type? eventMiddleware = null)
    {
        _consumerInterceptor = consumerInterceptor;
        _eventMiddleware = ValidateOrGetMiddleware(eventMiddleware);
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

        if (!type.IsAssignableTo(typeof(IEvent)) && !type.IsAssignableTo(typeof(ICommand)))
        {
            return;
        }

        IConsumerOptions? consumerAttribute = type.GetCustomAttribute<FastEndpointsConsumerAttribute>();
        if (consumerAttribute == null || string.IsNullOrWhiteSpace(consumerAttribute.Queue))
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

        if (_consumers.Any(x => x.Queue == consumerAttribute.Queue))
        {
            throw new ArgumentException($"Multiple consumers are bound to the same queue. queue: [{consumerAttribute.Queue}], event: {type.Name}");
        }

        Type consumerInterface = typeof(IConsumer<>).MakeGenericType(type);
        Type implementationType = typeof(FastEndpointsConsumer<>).MakeGenericType(type);

        services.TryAddEnumerable(ServiceDescriptor.Scoped(consumerInterface, implementationType));
        services.TryAdd(ServiceDescriptor.Scoped(implementationType, implementationType));

        Type middlewareServiceType = typeof(IEventMiddleware<>).MakeGenericType(type);
        Type middlewareImplementationType = _eventMiddleware.MakeGenericType(type);
        services.TryAdd(ServiceDescriptor.Scoped(middlewareServiceType, middlewareImplementationType));

        ConsumerType consumerType = new()
        {
            Queue = consumerAttribute.Queue,
            Consumer = implementationType,
            Event = type,
            ConsumerOptions = consumerAttribute,
        };

        _consumers.Add(consumerType);
    }

    private static Type ValidateOrGetMiddleware(Type? eventMiddleware)
    {
        if (eventMiddleware == null)
        {
            return typeof(DefaultFastEndpointsEventMiddleware<>);
        }

        if (!eventMiddleware.IsGenericTypeDefinition
            || !ImplementsOpenGenericInterface(eventMiddleware, typeof(IEventMiddleware<>)))
        {
            throw new TypeLoadException($"The type {eventMiddleware.Name} is not a valid event middleware. Example: public class DefaultFastEndpointsEventMiddleware<TCommand> : IEventMiddleware<TCommand> {{ }}");
        }

        return eventMiddleware;
    }

    private static bool ImplementsOpenGenericInterface(Type implementation, Type openGenericInterface)
    {
        if (!openGenericInterface.IsInterface || !openGenericInterface.IsGenericTypeDefinition)
        {
            return false;
        }

        return implementation
            .GetInterfaces()
            .Any(item => item.IsGenericType && item.GetGenericTypeDefinition() == openGenericInterface);
    }
}
