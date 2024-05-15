using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件总线类型过滤器.
    /// </summary>
    public class EventBusTypeFilter : ITypeFilter
    {
        private readonly Dictionary<Type, EventInfo> Events = new();
        private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
            .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

        public EventBusTypeFilter()
        {
            ArgumentNullException.ThrowIfNull(AddHostedMethod);
        }

        /// <inheritdoc />
        public void Build(IServiceCollection services)
        {
            Dictionary<string, List<EventInfo>> eventGroup = new();

            foreach (var item in Events)
            {
                var eventType = item.Key;
                Type? hostType = null;

                if (item.Value.Middleware == null)
                {
                    item.Value.Middleware = typeof(DefaultEventMiddleware<>).MakeGenericType(eventType);
                }

                services.Add(new ServiceDescriptor(typeof(IConsumer<>).MakeGenericType(eventType), typeof(EventBusConsumer<>).MakeGenericType(eventType), ServiceLifetime.Transient));
                services.AddTransient(typeof(HandlerBroker<>).MakeGenericType(eventType));
                services.AddKeyedSingleton(serviceKey: item.Key, serviceType: typeof(EventInfo), implementationInstance: item.Value);

                // 分组
                if (!string.IsNullOrEmpty(item.Value.Group))
                {
                    if (!eventGroup.TryGetValue(item.Value.Group, out var group)) 
                    {
                        group = new List<EventInfo>();
                        eventGroup.Add(item.Value.Group, group);
                    }
                    group.Add(item.Value);

                    continue;
                }

                hostType = typeof(ConsumerHostSrvice<,>).MakeGenericType(typeof(EventBusConsumer<>).MakeGenericType(eventType), eventType);
                AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
            }

            // 分组处理器
            // todo: 设计HandlerBroker、EventBusConsumer、MultipleConsumerHostSrvice，支持多 queue 订阅
            foreach (var group in eventGroup)
            {
                var eventGroupInfo = new EventGroupInfo
                {
                    Group = group.Key,
                    EventInfos = new Dictionary<string, EventInfo>()
                };
                services.AddKeyedSingleton(serviceKey: group.Key, serviceType: typeof(EventGroupInfo), implementationInstance: eventGroupInfo);
                services.AddHostedService<EventGroupConsumerHostSrvice>(s =>
                {
                    return new EventGroupConsumerHostSrvice(
                        s,
                        s.GetRequiredService<DefaultConnectionOptions>(),
                        s.GetRequiredService<IJsonSerializer>(),
                        s.GetRequiredService<ILogger<EventGroupConsumerHostSrvice>>(),
                        s.GetRequiredService<IPolicyFactory>(),
                        eventGroupInfo
                        );
                });
            }
        }

        /// <inheritdoc />
        public void Filter(Type type, IServiceCollection services)
        {
            if (!type.IsClass)
            {
                return;
            }

            EventInfo eventInfo;
            Type eventType = null!;

            var middleware = type.GetInterfaces().Where(x => x.IsGenericType)
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventMiddleware<>));

            if (middleware != null)
            {
                services.AddTransient(middleware);
                eventType = middleware.GenericTypeArguments[0];
            }

            var handler = type.GetInterfaces().Where(x => x.IsGenericType)
                    .FirstOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            if (handler != null)
            {
                services.AddTransient(handler);
                eventType = handler.GenericTypeArguments[0];
            }

            if (eventType == null)
            {
                return;
            }

            if (!Events.TryGetValue(eventType, out eventInfo!))
            {
                var eventQueue = eventType.GetCustomAttribute<EventTopicAttribute>();
                if (eventQueue == null)
                {
                    throw new InvalidOperationException($"{eventType.Name} 没有配置 [EventQueue] 特性");
                }

                eventInfo = new EventInfo
                {
                    EventType = eventType,
                    Queue = eventQueue.Queue,
                    Qos = eventQueue.Qos,
                    Group = eventQueue.Group,
                    Requeue = eventQueue.Requeue,
                    Handlers = new Dictionary<int, Type>()
                };
            }

            if (middleware != null)
            {
                eventInfo.Middleware = middleware;
            }

            if (handler != null)
            {
                var eventOrder = handler.GetCustomAttribute<EventOrderAttribute>();
                if (eventOrder == null)
                {
                    throw new InvalidOperationException($"{handler.Name} 没有配置 [EventOrder] 特性");
                }
                if (!eventInfo.Handlers.TryAdd(eventOrder.Order, handler))
                {
                    throw new InvalidOperationException($"{eventInfo.Handlers[eventOrder.Order].Name} 与 {handler.Name} 的 Order 重复");
                }
            }
        }
    }
}
