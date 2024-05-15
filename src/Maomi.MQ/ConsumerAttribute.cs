namespace Maomi.MQ
{
    /// <summary>
    /// 消费者配置.
    /// </summary>

    [AttributeUsage(AttributeTargets.Class,AllowMultiple = false,Inherited = true)]
    public class ConsumerAttribute : Attribute
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }

        private ushort _qos = 10;

        /// <summary>
        /// 当消息消费失败时，是否重新放回队列，当 <see cref="Qos"/> = 1 时，此配置无效.
        /// </summary>
        public bool Requeue { get; set; }

        public ConsumerAttribute(string queue)
        {
            Queue = queue;
        }

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
    }
}
