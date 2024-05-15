using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件中介者，用于生成有顺序的事件执行流程和补偿流程.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class HandlerBroker<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EventInfo _eventInfo;

        public HandlerBroker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _eventInfo = _serviceProvider.GetRequiredKeyedService<EventInfo>(typeof(TEvent));
        }

        /// <summary>
        /// 执行事件，该方法会被生成 <see cref="EventHandlerDelegate{TEvent}" />  委托，传递到 <see cref="IEventMiddleware{TEvent}"/> 中.
        /// </summary>
        /// <param name="eventBody"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handler(EventBody<TEvent> eventBody, CancellationToken cancellationToken)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<TEvent>>();
            List<IEventHandler<TEvent>> eventHandlers = new List<IEventHandler<TEvent>>(_eventInfo.Handlers.Count);

            foreach (var handler in _eventInfo.Handlers)
            {
                // 构建执行链
                try
                {
                    var eventHandler = _serviceProvider.GetRequiredService(handler.Value) as IEventHandler<TEvent>;
                    eventHandlers.Add(eventHandler);
                    await eventHandler.HandlerAsync(eventBody, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
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
}
