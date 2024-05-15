namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件执行器接口.
    /// </summary>
    /// <typeparam name="TEvent">事件模型.</typeparam>
    public interface IEventHandler<TEvent>
    {
        /// <summary>
        /// 正向执行事件.
        /// </summary>
        /// <param name="eventBody">事件对象.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task HandlerAsync(EventBody<TEvent> eventBody, CancellationToken cancellationToken);

        /// <summary>
        /// 补偿事件.
        /// </summary>
        /// <param name="eventBody">事件对象.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CancelAsync(EventBody<TEvent> eventBody, CancellationToken cancellationToken);
    }
}
