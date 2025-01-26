using Maomi.MQ;
using Maomi.MQ.Pool;
using Moq;
using RabbitMQ.Client;
using Xunit;

public class ConnectionPoolTests
{
    [Fact]
    public void ConnectionPool_Create_ReturnsConnectionObject()
    {
        var mqOptions = new MqOptions
        {
            WorkId = 1,
            AppName = "TestApp",
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object
        };

        var connectionPool = new ConnectionPool(mqOptions);
        var connectionObject = connectionPool.Create();

        Assert.NotNull(connectionObject);
        Assert.IsType<ConnectionObject>(connectionObject);
    }

    [Fact]
    public void ConnectionPool_Get_ReturnsSameConnectionObject()
    {
        // Arrange
        var mqOptions = new MqOptions
        {
            WorkId = 1,
            AppName = "TestApp",
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object
        };

        // Act
        var connectionPool = new ConnectionPool(mqOptions);
        var connectionObject1 = connectionPool.Get();
        var connectionObject2 = connectionPool.Get();

        // Assert
        Assert.NotNull(connectionObject1);
        Assert.NotNull(connectionObject2);
        Assert.Same(connectionObject1, connectionObject2);
    }
}
