namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 事件分组，相同分组的队列会被放到同一个通道中消费.
    /// </summary>
    public class EventGroupInfo
    {
        /// <summary>
        /// 分组名称.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 事件列表.
        /// </summary>
        public Dictionary<string, EventInfo> EventInfos { get; set; }
    }
}
