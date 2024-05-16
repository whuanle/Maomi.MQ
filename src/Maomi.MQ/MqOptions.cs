using RabbitMQ.Client;

namespace Maomi.MQ
{
    /// <summary>
    /// 连接配置.
    /// </summary>
    public class MqOptions
    {
        /// <summary>
        /// 消息队列前缀，所有消息队列会自动加前缀.
        /// </summary>
        public string? QueuePrefix { get; set; }

        /// <summary>
        /// 0-1024
        /// </summary>
        public int WorkId { get; set; }
    }

    public class DefaultMqOptions: MqOptions
    {
        public ConnectionFactory ConnectionFactory { get; set; }
    }
}
