namespace Maomi.MQ
{
    /// <summary>
    /// 事件主题.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EventTopicAttribute : Attribute
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }

        public string? Group { get; set; }

        /// <summary>
        /// 失败后是否放回队列，当 Qos 为 1 时，此配置无效.
        /// </summary>
        public bool Requeue { get; set; }

        private ushort _qos = 10;


        public ushort Qos
        {
            get => _qos;
            set
            {
                if (value <= 0)
                {
                    _qos = 1;
                }
                else
                {
                    _qos = value;
                }
            }
        }

        public EventTopicAttribute(string queue)
        {
            Queue = queue;
        }
    }
}
