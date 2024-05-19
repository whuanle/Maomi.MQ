// <copyright file="EventBusTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
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
    private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
        .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

    private readonly Dictionary<Type, EventInfo> _eventInfos = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusTypeFilter"/> class.
    /// </summary>
    public EventBusTypeFilter()
    {
        ArgumentNullException.ThrowIfNull(AddHostedMethod);
    }

    /// <inheritdoc />
    public void Build(IServiceCollection services)
    {
        Dictionary<string, List<EventInfo>> eventGroups = new();

        foreach (var item in _eventInfos)
        {
            var eventType = item.Key;
            var eventInfo = item.Value;

            Type? hostType = null;

            // If there is no IEventMiddleware<T> interface implemented for an event, the default DefaultEventMiddleware<T> is used.
            if (eventInfo.Middleware == null)
            {
                eventInfo.Middleware = typeof(DefaultEventMiddleware<>).MakeGenericType(eventType);
            }

            services.AddTransient(typeof(HandlerMediator<>).MakeGenericType(eventType));

            services.AddKeyedSingleton(serviceKey: eventInfo.EventType, serviceType: typeof(ConsumerOptions), implementationInstance: new ConsumerOptions
            {
                Qos = eventInfo.Qos,
                Queue = eventInfo.Queue,
                RetryFaildRequeue = eventInfo.RetryFaildRequeue,
                ExecptionRequeue = eventInfo.ExecptionRequeue
            });

            services.Add(new ServiceDescriptor(
                serviceType: typeof(IEventMiddleware<>).MakeGenericType(eventType),
                implementationType: eventInfo.Middleware,
                lifetime: ServiceLifetime.Transient));

            services.Add(new ServiceDescriptor(
                serviceType: typeof(IConsumer<>).MakeGenericType(eventType),
                implementationType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                lifetime: ServiceLifetime.Transient));

            services.AddKeyedSingleton(serviceKey: item.Key, serviceType: typeof(EventInfo), implementationInstance: eventInfo);

            // Group.
            // Do not use EventBusConsumerHostSrvice<EventBusConsumer<T>,T>.
            if (!string.IsNullOrEmpty(eventInfo.Group))
            {
                if (!eventGroups.TryGetValue(eventInfo.Group, out var eventInfoList))
                {
                    eventInfoList = new List<EventInfo>();
                    eventGroups.Add(eventInfo.Group, eventInfoList);
                }

                eventInfoList.Add(eventInfo);

                continue;
            }

            // Use EventBusConsumerHostSrvice<EventBusConsumer<T>,T>.
            hostType = typeof(EventBusConsumerHostSrvice<,>).MakeGenericType(typeof(EventBusConsumer<>).MakeGenericType(eventType), eventType);
            AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
        }

        // Use EventGroupConsumerHostSrvice processing group of consumers.
        foreach (var group in eventGroups)
        {
            var eventGroupInfo = new EventGroupInfo
            {
                Group = group.Key,
                EventInfos = group.Value.ToDictionary(x => x.Queue, x => x)
            };

            services.AddKeyedSingleton(serviceKey: group.Key, serviceType: typeof(EventGroupInfo), implementationInstance: eventGroupInfo);
            services.AddHostedService<EventGroupConsumerHostSrvice>(s =>
            {
                return new EventGroupConsumerHostSrvice(
                    s,
                    s.GetRequiredService<DefaultMqOptions>(),
                    s.GetRequiredService<IJsonSerializer>(),
                    s.GetRequiredService<ILogger<EventGroupConsumerHostSrvice>>(),
                    s.GetRequiredService<IRetryPolicyFactory>(),
                    s.GetRequiredService<IWaitReadyFactory>(),
                    eventGroupInfo);
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
            services.AddTransient(type);
            eventType = handlerInterface.GenericTypeArguments[0];
        }

        // IEventMiddleware<T> and IEventHandler<T> are not found.
        if (eventType == null)
        {
            return;
        }

        EventInfo eventInfo;

        if (!_eventInfos.TryGetValue(eventType, out eventInfo!))
        {
            var eventTopicAttribute = eventType.GetCustomAttribute<EventTopicAttribute>();
            if (eventTopicAttribute == null)
            {
                throw new ArgumentNullException($"{eventType.Name} type is not configured with the [EventTopic] attribute.");
            }

            eventInfo = new EventInfo
            {
                EventType = eventType,
                Queue = eventTopicAttribute.Queue,
                Qos = eventTopicAttribute.Qos,
                Group = eventTopicAttribute.Group,
                RetryFaildRequeue = eventTopicAttribute.RetryFaildRequeue,
                ExecptionRequeue = eventTopicAttribute.ExecptionRequeue,
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
}
