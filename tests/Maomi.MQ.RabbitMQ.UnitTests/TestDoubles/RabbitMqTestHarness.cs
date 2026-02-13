using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Models;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;

internal sealed class RabbitMqTestHarness
{
    public RabbitMqTestHarness()
    {
        ConnectionFactoryMock = new Mock<IConnectionFactory>(MockBehavior.Strict);
        ConnectionMock = new Mock<IConnection>(MockBehavior.Strict);
        SharedChannelMock = new Mock<IChannel>(MockBehavior.Strict);

        SetupDefaultChannelBehavior(SharedChannelMock);

        ConnectionMock
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SharedChannelMock.Object);

        ConnectionFactoryMock
            .Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConnectionMock.Object);

        MqOptions = new MqOptions
        {
            AppName = "unit-test-app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = ConnectionFactoryMock.Object,
            MessageSerializers =
            [
                new FakeMessageSerializer("application/json")
            ]
        };

        ConnectionPool = new ConnectionPool(MqOptions);

        ServiceCollection = new ServiceCollection();
        ServiceCollection.AddLogging();
        ServiceCollection.AddSingleton(MqOptions);
        ServiceCollection.AddSingleton(ConnectionPool);
        ServiceCollection.AddSingleton<IIdProvider>(new DefaultIdProvider(1));
        ServiceCollection.AddSingleton<IRetryPolicyFactory, AlwaysRetryOncePolicyFactory>();
        ServiceCollection.AddSingleton<IConsumerDiagnostics>(new Mock<IConsumerDiagnostics>().Object);
        ServiceCollection.AddSingleton<IPublisherDiagnostics>(new Mock<IPublisherDiagnostics>().Object);
        ServiceCollection.AddSingleton<IRoutingProvider, RoutingProvider>();
        ServiceCollection.AddScoped<IBreakdown, DefaultBreakdown>();
        ServiceCollection.AddScoped<ServiceFactory>();
    }

    public IServiceCollection ServiceCollection { get; }

    public MqOptions MqOptions { get; }

    public ConnectionPool ConnectionPool { get; }

    public Mock<IConnectionFactory> ConnectionFactoryMock { get; }

    public Mock<IConnection> ConnectionMock { get; }

    public Mock<IChannel> SharedChannelMock { get; }

    public Mock<IChannel> CreateAdditionalChannel(string consumerTag = "ctag-1")
    {
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        SetupDefaultChannelBehavior(channelMock, consumerTag);

        ConnectionMock
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelMock.Object);

        return channelMock;
    }

    public IServiceProvider BuildProvider()
    {
        return ServiceCollection.BuildServiceProvider();
    }

    private static void SetupDefaultChannelBehavior(Mock<IChannel> channelMock, string consumerTag = "ctag-1")
    {
        channelMock
            .Setup(x => x.QueueDeclareAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueDeclareOk("q", 0, 0));

        channelMock
            .Setup(x => x.ExchangeDeclareAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock
            .Setup(x => x.QueueBindAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock
            .Setup(x => x.BasicQosAsync(
                It.IsAny<uint>(),
                It.IsAny<ushort>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock
            .Setup(x => x.BasicConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(consumerTag);

        channelMock
            .Setup(x => x.BasicCancelAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock
            .Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channelMock
            .Setup(x => x.BasicAckAsync(
                It.IsAny<ulong>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channelMock
            .Setup(x => x.BasicNackAsync(
                It.IsAny<ulong>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channelMock
            .Setup(x => x.TxSelectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        channelMock
            .Setup(x => x.TxCommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        channelMock
            .Setup(x => x.TxRollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channelMock.Setup(x => x.Dispose());
    }
}
