using Maomi.MQ;
using Maomi.MQ.Pool;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Pool;

public class ConnectionObjectTests
{
    [Fact]
    public void ConnectionAndDefaultChannel_ShouldBeLazilyCreated()
    {
        var connectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        connectionFactory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>())).ReturnsAsync(channel.Object);

        var options = new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = connectionFactory.Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        };

        var obj = new ConnectionObject(options);

        var conn = obj.Connection;
        var ch = obj.DefaultChannel;

        Assert.Same(connection.Object, conn);
        Assert.Same(channel.Object, ch);
    }

    [Fact]
    public void Dispose_ShouldDisposeCreatedResources()
    {
        var connectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        connectionFactory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>())).ReturnsAsync(channel.Object);
        channel.Setup(x => x.Dispose());
        connection.Setup(x => x.Dispose());

        var options = new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = connectionFactory.Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        };

        var obj = new ConnectionObject(options);
        _ = obj.Connection;
        _ = obj.DefaultChannel;

        obj.Dispose();

        channel.Verify(x => x.Dispose(), Times.Once);
        connection.Verify(x => x.Dispose(), Times.Once);
    }
}
