namespace Maomi.MQ.EventBus
{
    /// <inheritdoc />
    public class DefaultEventMiddleware<TEvent> : IEventMiddleware<TEvent>
    {
        /// <inheritdoc />
        public Task HandleAsync(EventBody<TEvent> eventBody, EventHandlerDelegate<TEvent> next)
        {
            return next(eventBody, CancellationToken.None);
        }
    }
}
