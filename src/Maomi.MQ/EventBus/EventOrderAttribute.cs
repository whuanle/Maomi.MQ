namespace Maomi.MQ.EventBus
{
    /// <summary>
    /// 标识事件执行器的顺序.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventOrderAttribute : Attribute
    {
        /// <summary>
        /// 事件执行序号.
        /// </summary>
        public int Order { get; set; } = 0;

        public EventOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
