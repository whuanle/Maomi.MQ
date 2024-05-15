namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件信息.
    /// </summary>
    public class EventInfo
    {
        /// <summary>
        /// 队列 Qos.
        /// </summary>
        public int Qos { get; internal set; }

        /// <summary>
        /// 事件对应的队列名称.
        /// </summary>
        public string Queue { get; internal set; } = null!;

        /// <summary>
        /// 事件模型类型.
        /// </summary>
        public Type EventType { get; internal set; } = null!;

        /// <summary>
        /// 中间件类型.
        /// </summary>
        public Type Middleware { get; internal set; } = null!;

        /// <summary>
        /// 分组.
        /// </summary>
        public string? Group { get; internal set; }

        /// <summary>
        /// 消费失败次数达到条件时，是否放回队列.
        /// </summary>
        public bool Requeue { get; internal set; }

        /// <summary>
        /// 事件的执行器.
        /// </summary>
        public SortedDictionary<int, Type> Handlers { get; internal set; } = null!;
    }
}
