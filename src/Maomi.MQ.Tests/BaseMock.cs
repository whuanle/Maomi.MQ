using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using System.Reflection;

namespace Maomi.MQ.Tests;
public class BaseMock
{
    public readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    public readonly Mock<IConnection> _mockConnection = new Mock<IConnection>();
    public readonly Mock<IChannel> _mockChannel = new Mock<IChannel>();

    public BaseMock()
    {
        _mockConnectionFactory
            .Setup(c => c.CreateConnectionAsync(CancellationToken.None))
            .Returns(Task.FromResult(_mockConnection.Object));
        _mockConnection
            .Setup(c => c.CreateChannelAsync(CancellationToken.None))
            .Returns(Task.FromResult(_mockChannel.Object));

        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicQosAsync(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
    }

    public ServiceCollection Mock()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // mock.
        services.AddSingleton(_mockConnectionFactory.Object);
        services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
            options.Rabbit = rabbit => { };
        }, Array.Empty<Assembly>());
        services.AddSingleton(new MqOptions
        {
            AppName = "test",
            WorkId = 0,
            ConnectionFactory = _mockConnectionFactory.Object
        });
        return services;
    }

}
