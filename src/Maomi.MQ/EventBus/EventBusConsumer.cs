using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件消费者.
    /// </summary>
    /// <typeparam name="TEvent">事件模型.</typeparam>
    public class EventBusConsumer<TEvent> : IConsumer<TEvent>
        where TEvent : class
    {
        private readonly IEventMiddleware<TEvent> _eventMiddleware;
        private readonly HandlerBroker<TEvent> _handlerBroker;
        private readonly ILogger<EventBusConsumer<TEvent>> _logger;

        public EventBusConsumer(IEventMiddleware<TEvent> eventMiddleware, HandlerBroker<TEvent> handlerBroker, ILogger<EventBusConsumer<TEvent>> logger)
        {
            _eventMiddleware = eventMiddleware;
            _handlerBroker = handlerBroker;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(EventBody<TEvent> message)
        {
            // todo: 日志
            await _eventMiddleware.HandleAsync(message, _handlerBroker.Handler);
        }

        /// <inheritdoc />
        public Task FaildAsync(EventBody<TEvent> message)
        {
            // todo: 日志
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task FallbackAsync(EventBody<TEvent> message)
        {
            // todo: 日志
            return Task.CompletedTask;
        }
    }
}
