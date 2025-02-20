using Maomi.MQ.Default;
using Maomi.MQ.Pool;
using Moq;
using Moq.Protected;
using RabbitMQ.Client;

namespace Maomi.MQ.Tests;

public class RabbitMQConnectionMock
{
    public readonly MqOptions MqOptions;
    public readonly Mock<IConnectionFactory> ConnectionFactory;
    public readonly Mock<ConnectionPool> ConnectionPoolMock;
    public readonly Mock<ConnectionObject> ConnectionObjectMock;
    public readonly Mock<IConnection> ConnectionMock;
    public readonly Mock<IChannel> ChannelMock;

    public RabbitMQConnectionMock()
    {
        ChannelMock = new Mock<IChannel>();
        ConnectionMock = new Mock<IConnection>();
        ConnectionMock.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(ChannelMock.Object));
        ConnectionFactory = new();
        ConnectionFactory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(ConnectionMock.Object));

        MqOptions = new MqOptions
        {
            ConnectionFactory = ConnectionFactory.Object,
            AutoQueueDeclare = true,
            AppName = "Maomi.MQ",
            WorkId = 0
        };

        ConnectionObjectMock = new Mock<ConnectionObject>(MqOptions);

        ConnectionPoolMock = new Mock<ConnectionPool>(MqOptions);
    }
}
