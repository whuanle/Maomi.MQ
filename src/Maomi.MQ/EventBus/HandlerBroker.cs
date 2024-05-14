using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus
{
    public class HandlerBroker<TEvent>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EventInfo _eventInfo;

        public HandlerBroker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _eventInfo = _serviceProvider.GetRequiredKeyedService<EventInfo>(typeof(TEvent));
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

    // todo: queue 分组
}
