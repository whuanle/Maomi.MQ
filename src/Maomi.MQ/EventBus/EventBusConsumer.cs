namespace Maomi.MQ.EventBus
{
    public class EventBusConsumer<TEvent> : IConsumer<TEvent>
        where TEvent : class
    {
        private readonly IEventMiddleware<TEvent> _eventMiddleware;
        private readonly HandlerBroker<TEvent> _handlerBroker;

        public EventBusConsumer(IEventMiddleware<TEvent> eventMiddleware, HandlerBroker<TEvent> handlerBroker)
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

    // todo: queue 分组
}
