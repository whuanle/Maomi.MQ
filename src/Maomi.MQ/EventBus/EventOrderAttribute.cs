namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 标识类中有事件执行器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventOrderAttribute : Attribute
    {
        /// <summary>
        /// 事件排序
        /// </summary>
        public int Order { get; set; } = 0;

        public EventOrderAttribute(int order)
        {
            Order = order;
        }
    }

    // todo: queue 分组
}
