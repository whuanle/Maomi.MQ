// <copyright file="EventBusTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Eventbus type filter.<br />
/// 事件总线类型过滤器.
/// </summary>
public class EventBusTypeFilter : ITypeFilter
{
    private readonly Dictionary<Type, EventInfo> _eventInfos = new();
    private readonly ConsumerInterceptor? _eventTopicInterceptor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor"></param>
    public EventBusTypeFilter(ConsumerInterceptor? consumerInterceptor = null)
    {
        _eventTopicInterceptor = consumerInterceptor;
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
        IConsumerOptions? consumerOptions = null;
        var middlewareInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventMiddleware<>));

        if (middlewareInterface != null)
        {
            eventType = middlewareInterface.GenericTypeArguments[0];
            services.AddScoped(type);

            consumerOptions = type.GetCustomAttribute<ConsumerAttribute>();
            if (consumerOptions == null)
            {
                return;
            }

            if (_eventTopicInterceptor != null)
            {
                var register = _eventTopicInterceptor.Invoke(consumerOptions, type);
                if (!register.IsRegister)
                {
                    return;
                }

                consumerOptions.CopyFrom(register.ConsumerOptions);
            }
        }

        var handlerInterface = type.GetInterfaces().Where(x => x.IsGenericType)
                .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventHandler<>));

        if (handlerInterface != null)
        {
            eventType = handlerInterface.GenericTypeArguments[0];
            services.AddScoped(type);
        }

        if (eventType == null)
        {
            return;
        }

        EventInfo eventInfo;

        if (!_eventInfos.TryGetValue(eventType, out eventInfo!))
        {
            eventInfo = new EventInfo()
            {
                EventType = eventType
            };
            _eventInfos.Add(eventType, eventInfo);
        }

        if (middlewareInterface != null)
        {
            eventInfo.Middleware = type;
            eventInfo.Queue = consumerOptions!.Queue;
            eventInfo.Options = consumerOptions;
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

    /// <inheritdoc />
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        foreach (var item in _eventInfos)
        {
            var eventType = item.Key;
            var eventInfo = item.Value;

            if (eventInfo.Middleware == null)
            {
                continue;
            }

            // Singleton.
            AddIEventHandlerFactory(services, eventType, item.Value.Handlers);

            services.Add(new ServiceDescriptor(
                serviceType: typeof(IEventMiddleware<>).MakeGenericType(eventType),
                implementationType: eventInfo.Middleware,
                lifetime: ServiceLifetime.Scoped));

            services.AddScoped(serviceType: typeof(IHandlerMediator<>).MakeGenericType(eventType), implementationType: typeof(HandlerMediator<>).MakeGenericType(eventType));

            services.Add(new ServiceDescriptor(
                serviceType: typeof(IConsumer<>).MakeGenericType(eventType),
                implementationType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                lifetime: ServiceLifetime.Scoped));

            services.Add(new ServiceDescriptor(
                serviceType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                implementationType: typeof(EventBusConsumer<>).MakeGenericType(eventType),
                lifetime: ServiceLifetime.Scoped));
        }

        if (_eventInfos.Count == 0)
        {
            return Array.Empty<ConsumerType>();
        }

        var consumerTypes = _eventInfos.Select(x => new ConsumerType
        {
            Queue = x.Value.Options.Queue,
            Consumer = typeof(EventBusConsumer<>).MakeGenericType(x.Value.EventType),
            Event = x.Value.EventType,
            ConsumerOptions = x.Value.Options,
        }).ToList();

        return consumerTypes;
    }

    private static readonly MethodInfo AddIEventHandlerFactoryMethod = typeof(EventBusTypeFilter).GetMethod(nameof(AddSingletonIEventHandlerFactory), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private void AddSingletonIEventHandlerFactory<TMessage>(IServiceCollection services, SortedDictionary<int, Type> handlers)
        where TMessage : class
    {
        services.AddSingleton<IEventHandlerFactory<TMessage>>(s =>
        {
            return new EventHandlerFactory<TMessage>(handlers);
        });
    }

    private void AddIEventHandlerFactory(IServiceCollection services, Type messageType, SortedDictionary<int, Type> handlers)
    {
        var instanceParameter = Expression.Constant(this);
        var servicesParameter = Expression.Constant(services);
        var handlersParameter = Expression.Constant(handlers);

        var genericMethodInfo = AddIEventHandlerFactoryMethod.MakeGenericMethod(messageType);

        var methodCallExpression = Expression.Call(instanceParameter, genericMethodInfo, servicesParameter, handlersParameter);
        var lambda = Expression.Lambda<Action>(methodCallExpression);
        var action = lambda.Compile();
        action();
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
        /// <see cref="IEventMiddleware{TMessage}"/>.
        /// </summary>
        public Type Middleware { get; internal set; } = null!;

        /// <summary>
        /// Event handler.
        /// </summary>
        public SortedDictionary<int, Type> Handlers { get; private set; } = new();
    }
}
