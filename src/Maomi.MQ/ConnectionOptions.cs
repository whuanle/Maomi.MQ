using RabbitMQ.Client;

namespace Maomi.MQ
{
    public class ConnectionOptions
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

    public class DefaultConnectionOptions: ConnectionOptions
    {
        public ConnectionFactory ConnectionFactory { get; set; }
    }
}
