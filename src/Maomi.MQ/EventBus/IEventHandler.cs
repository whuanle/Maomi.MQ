namespace Maomi.MQ.EventBus
{
    public interface IEventHandler<TEvent>
    {
        Task HandlerAsync(EventBody<TEvent> @event, CancellationToken cancellationToken);
        Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken);
    }

    // todo: queue 分组
}
