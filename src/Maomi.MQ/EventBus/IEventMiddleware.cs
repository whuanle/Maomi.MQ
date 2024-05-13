using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ.EventBus
{
    // 定义事件委托，用于构建执行链
    public delegate Task EventHandlerDelegate<TEvent>(EventBody<TEvent> @event, CancellationToken cancellationToken);
    public interface IEventMiddleware<TEvent>
    {
        // @event: 事件
        // next: 下一个要执行的函数
        Task HandleAsync(EventBody<TEvent> @event, EventHandlerDelegate<TEvent> next);
    }


    public class DefaultEventMiddleware<TEvent> : IEventMiddleware<TEvent>
    {
        public Task HandleAsync(EventBody<TEvent> @event, EventHandlerDelegate<TEvent> next)
        {
            return next(@event, CancellationToken.None);
        }
    }

    /// <summary>
    /// 标识类中有事件执行器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventOrderAttribute : Attribute
    {
        /// <summary>
        /// 事件排序
        /// </summary>
        public int Order { get; set; } = 0;

        public EventOrderAttribute(int order)
        {
            Order = order;
        }
    }

    public interface IEventHandler<TEvent>
    {
        Task HandlerAsync(EventBody<TEvent> @event, CancellationToken cancellationToken);
        Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken);
    }

    public class EventBusSingleConsumer<TEvent> : ISingleConsumer<TEvent>
        where TEvent : class
    {
        private readonly IEventMiddleware<TEvent> _eventMiddleware;
        private readonly HandlerBroker<TEvent> _handlerBroker;

        public EventBusSingleConsumer(IEventMiddleware<TEvent> eventMiddleware, HandlerBroker<TEvent> handlerBroker)
        {
            _eventMiddleware = eventMiddleware;
            _handlerBroker = handlerBroker;
        }

        public async Task ExecuteAsync(EventBody<TEvent> message)
        {
            await _eventMiddleware.HandleAsync(message, _handlerBroker.Handler);
        }

        public Task FaildAsync(EventBody<TEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FallbackAsync(EventBody<TEvent> message)
        {
            throw new NotImplementedException();
        }
    }

    public class EventBusMultipleConsumer<TEvent> : IMultipleConsumer<TEvent>
        where TEvent : class
    {
        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(EventBody<TEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FallbackAsync(EventBody<TEvent> message)
        {
            throw new NotImplementedException();
        }
    }
    public class EventBusTypeFilter : ITypeFilter
    {
        public readonly Dictionary<Type, EventInfo> Events = new();
        private static readonly MethodInfo AddHostedMethod = typeof(ServiceCollectionHostedServiceExtensions)
            .GetMethod(nameof(ServiceCollectionHostedServiceExtensions.AddHostedService), BindingFlags.Static | BindingFlags.Public, [typeof(IServiceCollection)])!;

        public EventBusTypeFilter()
        {
            ArgumentNullException.ThrowIfNull(AddHostedMethod);
        }


        public void Build(IServiceCollection services)
        {
            foreach (var item in Events)
            {
                var eventType = item.Key;
                Type? hostType = null;

                if (item.Value.Middleware == null)
                {
                    item.Value.Middleware = typeof(DefaultEventMiddleware<>).MakeGenericType(eventType);
                }
                if (item.Value.Qos == 1)
                {
                    hostType = typeof(SingleHostSrvice<,>).MakeGenericType(typeof(EventBusSingleConsumer<>).MakeGenericType(eventType), eventType);
                    services.Add(new ServiceDescriptor(typeof(ISingleConsumer<>).MakeGenericType(eventType), typeof(EventBusSingleConsumer<>).MakeGenericType(eventType), ServiceLifetime.Transient));
                }
                else
                {
                    hostType = typeof(MultipleHostSrvice<,>).MakeGenericType(typeof(EventBusMultipleConsumer<>).MakeGenericType(eventType), eventType);
                    services.Add(new ServiceDescriptor(typeof(IMultipleConsumer<>).MakeGenericType(eventType), typeof(EventBusMultipleConsumer<>).MakeGenericType(eventType), ServiceLifetime.Transient));
                }

                if (hostType != null)
                {
                    AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });
                }
                AddHostedMethod.MakeGenericMethod(hostType).Invoke(null, new object[] { services });

                services.AddTransient(typeof(HandlerBroker<>).MakeGenericType(eventType));
            }

            // todo: build 时生成调用链
            // EventBusSingleConsumer 生成处理代码
        }




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
    public class HandlerBroker<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EventInfo _eventInfo;

        public HandlerBroker(IServiceProvider serviceProvider, EventInfo eventInfo)
        {
            _serviceProvider = serviceProvider;
            _eventInfo = eventInfo;
        }

        public async Task Handler(EventBody<TEvent> eventBody, CancellationToken cancellationToken)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<TEvent>>();
            List<IEventHandler<TEvent>> eventHandlers = new List<IEventHandler<TEvent>>(_eventInfo.Handlers.Count);

            foreach (var handlerType in _eventInfo.Handlers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // 构建执行链
                try
                {
                    var eventHandler = _serviceProvider.GetRequiredService(handlerType.Value) as IEventHandler<TEvent>;
                    eventHandlers.Add(eventHandler);
                    await eventHandler.HandlerAsync(eventBody, cancellationToken);
                }
                // 执行失败，开始回退
                catch (Exception ex)
                {
                    for (int j = eventHandlers.Count - 1; j >= 0; j--)
                    {
                        var eventHandler = eventHandlers[j];
                        await eventHandler.CancelAsync(eventBody, cancellationToken);
                    }
                    return;
                }
            }
        }
    }

    public class EventInfo
    {
        public int Qos { get; internal set; }
        public string Queue { get; internal set; }
        public Type EventType { get; internal set; }
        public Type Middleware { get; internal set; }
        public Dictionary<int, Type> Handlers { get; internal set; }
    }

    // 合并 ISingleConsumer、IMultipleConsumer 为一个
    // EventInfo 注入问题
    // 增加配置，最后失败时，是否重新放回队列
    // todo: queue 分组
    // GetRequiredKeyedService
}
