namespace Maomi.MQ.EventBus
{
    public class DefaultEventMiddleware<TEvent> : IEventMiddleware<TEvent>
    {
        public Task HandleAsync(EventBody<TEvent> @event, EventHandlerDelegate<TEvent> next)
        {
            return next(@event, CancellationToken.None);
        }
    }

    // todo: queue 分组
}
