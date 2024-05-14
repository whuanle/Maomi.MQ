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
}
