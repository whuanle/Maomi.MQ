namespace Maomi.MQ
{
    /// <summary>
    /// 事件消息体.
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public class EventBody<TEvent>
    {
        /// <summary>
        /// 事件唯一 id.
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// 事件创建时间.
        /// </summary>
        public DateTimeOffset CreateTime { get; init; }

        /// <summary>
        /// 事件体.
        /// </summary>
        public TEvent Body { get; init; }
    }
}
