using RabbitMQ.Client;

namespace Maomi.MQ.Pool
{
    /// <summary>
    /// TCP 连接和通道.
    /// </summary>
    public class ConnectionObject : IDisposable
    {
        private readonly DefaultMqOptions _connectionOptions;
        public readonly IConnection _connection;
        private readonly IChannel _channel;

        public IConnection Connection => _connection;
        public IChannel Channel => _channel;

        public ConnectionObject(DefaultMqOptions connectionOptions)
        {
            _connectionOptions = connectionOptions;
            _connection = connectionOptions.ConnectionFactory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
