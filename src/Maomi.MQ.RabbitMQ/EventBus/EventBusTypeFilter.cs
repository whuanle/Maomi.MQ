﻿// <copyright file="EventBusTypeFilter.cs" company="Maomi">
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

namespace Maomi.MQ.EventBus;

/// <summary>
/// <see cref="EventTopicAttribute"/> filter.
/// </summary>
/// <remarks>You can modify related parameters.<br />可以修改相关参数.</remarks>
/// <param name="eventTopicAttribute"></param>
/// <param name="eventType"></param>
/// <returns>Whether to register the event.<br />是否注册该事件.</returns>
public delegate bool EventTopicInterceptor(EventTopicAttribute eventTopicAttribute, Type eventType);

/// <summary>
/// Eventbus type filter.<br />
/// 事件总线类型过滤器.
/// </summary>
public class EventBusTypeFilter : ITypeFilter
{
    private readonly Dictionary<Type, EventInfo> _eventInfos = new();
    private readonly EventTopicInterceptor? _eventTopicInterceptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor"></param>
    public EventBusTypeFilter(EventTopicInterceptor? consumerInterceptor = null)
    {
        _eventTopicInterceptor = consumerInterceptor;
    }

    /// <inheritdoc />
    public void Build(IServiceCollection services)
    {
        foreach (var item in _eventInfos)
        {
            var eventType = item.Key;
            var eventInfo = item.Value;

            // If there is no IEventMiddleware<T> interface implemented for an event, the default DefaultEventMiddleware<T> is used.
            if (eventInfo.Middleware == null)
            {
                eventInfo.Middleware = typeof(DefaultEventMiddleware<>).MakeGenericType(eventType);
            }

            // Singleton.
            var handlerFactory = (Activator.CreateInstance(typeof(EventHandlerFactory<>).MakeGenericType(eventType), item.Value.Handlers) as IEventHandlerFactory)!;
            services.Add(new ServiceDescriptor(
                serviceType: typeof(IEventHandlerFactory<>).MakeGenericType(eventType),
                instance: handlerFactory));

            services.Add(new ServiceDescriptor(
                serviceType: typeof(IEventMiddleware<>).MakeGenericType(eventType),
                implementationType: eventInfo.Middleware,
                lifetime: ServiceLifetime.Scoped));

            services.AddScoped(serviceType: typeof(IHandlerMediator<>).MakeGenericType(eventType), implementationType: typeof(HandlerMediator<>).MakeGenericType(eventType));

            services.AddKeyedSingleton(serviceKey: eventInfo.Queue, serviceType: typeof(IConsumerOptions), implementationInstance: eventInfo.Options);

            services.Add(new ServiceDescriptor(
                serviceKey: eventInfo.Options.Queue,
                serviceType: typeof(IConsumer<>).MakeGenericType(eventType),
                implementationType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                lifetime: ServiceLifetime.Scoped));
        }

        if (_eventInfos.Count == 0)
        {
            return;
        }

        var consumerTypes = _eventInfos.Select(x => new ConsumerType
        {
            Queue = x.Value.Options.Queue,
            Consumer = typeof(EventBusConsumer<>).MakeGenericType(x.Value.EventType),
            Event = x.Value.EventType
        }).ToList();

        Func<IServiceProvider, EventBusHostService> funcFactory = (serviceProvider) =>
        {
            return new EventBusHostService(
                serviceProvider,
                serviceProvider.GetRequiredService<ServiceFactory>(),
                serviceProvider.GetRequiredService<ConnectionPool>(),
                serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                consumerTypes);
        };

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory, lifetime: ServiceLifetime.Singleton));
    }

    /// <inheritdoc />
    public void Filter(IServiceCollection services, Type type)
    {
        /*
           Filter the following types:
           IEventMiddleware<T>
           IEventHandler<T>
         */

        if (!type.IsClass)
        {
            return;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultEventMiddleware<>))
        {
            return;
        }

        Type? eventType = null;

        var middlewareInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventMiddleware<>));

        if (middlewareInterface != null)
        {
            eventType = middlewareInterface.GenericTypeArguments[0];
        }

        var handlerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventHandler<>));

        if (handlerInterface != null)
        {
            eventType = handlerInterface.GenericTypeArguments[0];
        }

        // IEventMiddleware<T> and IEventHandler<T> are not found.
        if (eventType == null)
        {
            if (type.GetCustomAttribute<EventTopicAttribute>() != null)
            {
                eventType = type;
            }
            else
            {
                return;
            }
        }

        var eventTopicAttribute = eventType.GetCustomAttribute<EventTopicAttribute>();
        if (eventTopicAttribute == null)
        {
            return;
        }

        if (handlerInterface != null)
        {
            // type: IEventHandler`1
            services.AddScoped(type);
        }

        if (_eventTopicInterceptor != null)
        {
            var isRegister = _eventTopicInterceptor.Invoke(eventTopicAttribute, type);
            if (!isRegister)
            {
                return;
            }
        }

        EventInfo eventInfo;

        if (!_eventInfos.TryGetValue(eventType, out eventInfo!))
        {
            eventInfo = new EventInfo
            {
                Queue = eventTopicAttribute.Queue,
                Options = eventTopicAttribute,
                EventType = eventType,
            };
            _eventInfos.Add(eventType, eventInfo);
        }

        if (middlewareInterface != null)
        {
            eventInfo.Middleware = type;
        }

        if (handlerInterface != null)
        {
            var eventOrder = type.GetCustomAttribute<EventOrderAttribute>();
            if (eventOrder == null)
            {
                throw new ArgumentNullException($"{type.Name} type is not configured with the [EventOrder] attribute.");
            }

            if (!eventInfo.Handlers.TryAdd(eventOrder.Order, type))
            {
                throw new ArgumentException($"The Order values of {eventInfo.Handlers[eventOrder.Order].Name} and {type.Name} are repeated, with Order = {eventOrder.Order}.");
            }
        }
    }

    /// <summary>
    /// Event info.<br />
    /// 事件信息.
    /// </summary>
    private class EventInfo
    {
        /// <summary>
        /// Queue.
        /// </summary>
        public string Queue { get; set; } = null!;

        /// <summary>
        /// <see cref="IConsumerOptions"/>.
        /// </summary>
        public IConsumerOptions Options { get; set; } = null!;

        /// <summary>
        /// Event type.
        /// </summary>
        public Type EventType { get; internal set; } = null!;

        /// <summary>
        /// <see cref="IEventMiddleware{TEvent}"/>.
        /// </summary>
        public Type Middleware { get; internal set; } = null!;

        /// <summary>
        /// Event handler.
        /// </summary>
        public SortedDictionary<int, Type> Handlers { get; private set; } = new();
    }
}
