// <copyright file="EventBusTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Eventbus type filter.<br />
/// 事件总线类型过滤器.
/// </summary>
public class EventBusTypeFilter : ITypeFilter
{
    private readonly Dictionary<Type, EventInfo> _eventInfos = new();

    /// <inheritdoc />
    public void Build(IServiceCollection services)
    {
        Dictionary<string, List<EventInfo>> eventGroups = new();

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

            services.AddKeyedSingleton(serviceKey: eventInfo.Options.Queue, serviceType: typeof(IConsumerOptions), implementationInstance: eventInfo.Options);

            services.Add(new ServiceDescriptor(
                serviceKey: eventInfo.Options.Queue,
                serviceType: typeof(IConsumer<>).MakeGenericType(eventType),
                implementationType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                lifetime: ServiceLifetime.Scoped));

            if (!string.IsNullOrEmpty(eventInfo.Options.Group))
            {
                if (!eventGroups.TryGetValue(eventInfo.Options.Group, out var eventInfoList))
                {
                    eventInfoList = new List<EventInfo>();
                    eventGroups.Add(eventInfo.Options.Group, eventInfoList);
                }

                eventInfoList.Add(eventInfo);

                continue;
            }

            services.AddHostedService<ConsumerBaseHostService>(s =>
            {
                var consumerType = new ConsumerType
                {
                    Queue = eventInfo.Options.Queue,
                    Consumer = typeof(EventBusConsumer<>).MakeGenericType(eventInfo.EventType),
                    Event = eventInfo.EventType
                };
                return new ConsumerBaseHostService(
                    s,
                    s.GetRequiredService<ServiceFactory>(),
                    s.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                    new List<ConsumerType> { consumerType });
            });
        }

        // Use EventGroupConsumerHostSrvice processing group of consumers.
        foreach (var group in eventGroups)
        {
            var consumerTypes = group.Value.Select(x => new ConsumerType
            {
                Queue = x.Options.Queue,
                Consumer = typeof(EventBusConsumer<>).MakeGenericType(x.EventType),
                Event = x.EventType
            }).ToList();
            services.AddHostedService<ConsumerBaseHostService>(s =>
            {
                return new ConsumerBaseHostService(
                    s,
                    s.GetRequiredService<ServiceFactory>(),
                    s.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                    consumerTypes);
            });
        }
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
            services.AddScoped(type);
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
            throw new ArgumentNullException($"{eventType.Name} type is not configured with the [EventTopic] attribute.");
        }

        EventInfo eventInfo;

        if (!_eventInfos.TryGetValue(eventType, out eventInfo!))
        {
            eventInfo = new EventInfo
            {
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
