using IdGen;
using Maomi.MQ.Pool;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace Maomi.MQ.Defaults
{

    public class MessagePublisher : IMessagePublisher
    {
        private readonly DefaultConnectionOptions _connectionOptions;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ConnectionPool _connectionPool;
        private readonly IIdGenerator<long> _idGen;

        public MessagePublisher(DefaultConnectionOptions connectionOptions, IJsonSerializer jsonSerializer, ConnectionPool connectionPool, IIdGenerator<long> idGen)
        {
            _connectionOptions = connectionOptions;
            _jsonSerializer = jsonSerializer;
            _connectionPool = connectionPool;
            _idGen = idGen;
        }

        public async Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null)
            where TEvent : class
        {
            var basicProperties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };

            if (properties != null)
            {
                properties.Invoke(basicProperties);
            }

            var connection = _connectionPool.Get();

            // todo: 发布者的优化
            var eventBody = new EventBody<TEvent>
            {
                Id = _idGen.CreateId(),
                CreateTime = DateTimeOffset.Now,
                Body = message
            };
            try
            {
                await connection.Channel.BasicPublishAsync(exchange: string.Empty,
                    routingKey: queue,
                    basicProperties: basicProperties,
                    body: _jsonSerializer.Serializer(eventBody));
            }
            finally
            {
                _connectionPool.Return(connection);
            }
        }
    }
}
