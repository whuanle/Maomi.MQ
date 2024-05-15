namespace Maomi.MQ
{
    public class ConsumerOptions
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }
        public bool Requeue { get; set; }

        public ushort Qos { get; set; }
    }
}
