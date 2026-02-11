using Maomi.MQ;
using Maomi.MQ.Pool;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Pool;

public class ConnectionPoolTests
{
    [Fact]
    public void Get_ShouldReturnSingletonConnectionObject()
    {
        var pool = new ConnectionPool(CreateOptions());

        var a = pool.Get();
        var b = pool.Get();

        Assert.Same(a, b);
    }

    [Fact]
    public void Create_ShouldReturnNewConnectionObjectEachTime()
    {
        var pool = new ConnectionPool(CreateOptions());

        var a = pool.Create();
        var b = pool.Create();

        Assert.NotSame(a, b);
        a.Dispose();
        b.Dispose();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var pool = new ConnectionPool(CreateOptions());
        pool.Dispose();
    }

    private static MqOptions CreateOptions()
    {
        var connectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        connectionFactory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>())).ReturnsAsync(channel.Object);
        channel.Setup(x => x.Dispose());
        connection.Setup(x => x.Dispose());

        return new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = connectionFactory.Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        };
    }
}
