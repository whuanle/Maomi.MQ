using Maomi.MQ;
using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Hosts;

public class DynamicConsumerServiceTests
{
    [Fact]
    public async Task ConsumerAsync_WhenQueueAlreadyUsedByStaticConsumer_ShouldThrow()
    {
        var connectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var defaultChannel = CreateCommonChannelMock("ctag-default");

        connection
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultChannel.Object);

        connectionFactory
            .Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);

        var mqOptions = new MqOptions
        {
            AppName = "ut-app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = connectionFactory.Object,
            MessageSerializers = [new TestSerializer()],
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mqOptions);
        services.AddSingleton<IRetryPolicyFactory>(new Mock<IRetryPolicyFactory>().Object);
        services.AddSingleton<IIdProvider>(new DefaultIdProvider(1));
        services.AddSingleton<IConsumerDiagnostics>(new Mock<IConsumerDiagnostics>().Object);
        services.AddScoped<ServiceFactory>();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ServiceFactory>();
        var pool = new ConnectionPool(mqOptions);

        var providerWithConflict = new ConsumerTypeProvider(new[]
        {
            new ConsumerType
            {
                Queue = "queue-a",
                Consumer = typeof(ConflictConsumer),
                Event = typeof(TestMessage),
                ConsumerOptions = new ConsumerOptions { Queue = "queue-a" },
            }
        });

        var service = new DynamicConsumerService(factory, pool, providerWithConflict);
        var options = new ConsumerOptions { Queue = "queue-a", AutoQueueDeclare = AutoQueueDeclare.Enable };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ConsumerAsync<ConflictConsumer, TestMessage>(options));

        Assert.Contains("Queue[queue-a] have been used by consumer", ex.Message);
    }

    [Fact]
    public async Task StopConsumerTagAsync_WhenUnknownTag_ShouldCancelOnDefaultChannel()
    {
        var connectionFactory = new Mock<IConnectionFactory>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var defaultChannel = CreateCommonChannelMock("ctag-default");

        defaultChannel
            .Setup(x => x.BasicCancelAsync("unknown-tag", true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        connection
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultChannel.Object);

        connectionFactory
            .Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);

        var mqOptions = new MqOptions
        {
            AppName = "ut-app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = connectionFactory.Object,
            MessageSerializers = [new TestSerializer()],
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mqOptions);
        services.AddSingleton<IRetryPolicyFactory>(new Mock<IRetryPolicyFactory>().Object);
        services.AddSingleton<IIdProvider>(new DefaultIdProvider(1));
        services.AddSingleton<IConsumerDiagnostics>(new Mock<IConsumerDiagnostics>().Object);
        services.AddScoped<ServiceFactory>();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ServiceFactory>();
        var pool = new ConnectionPool(mqOptions);

        var service = new DynamicConsumerService(factory, pool, new ConsumerTypeProvider());

        await service.StopConsumerTagAsync("unknown-tag");

        defaultChannel.Verify();
    }

    private static Mock<IChannel> CreateCommonChannelMock(string consumerTag)
    {
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        channel
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

        channel
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

        channel
            .Setup(x => x.QueueBindAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel
            .Setup(x => x.BasicQosAsync(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel
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

        channel
            .Setup(x => x.BasicCancelAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel
            .Setup(x => x.BasicAckAsync(
                It.IsAny<ulong>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channel
            .Setup(x => x.BasicNackAsync(
                It.IsAny<ulong>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        channel.Setup(x => x.Dispose());

        return channel;
    }

    private sealed class TestSerializer : IMessageSerializer
    {
        public string ContentType => "application/json";

        public bool SerializerVerify<TObject>(TObject obj) => true;

        public bool SerializerVerify<TObject>() => true;

        public byte[] Serializer<TObject>(TObject obj)
            => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);

        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
            => System.Text.Json.JsonSerializer.Deserialize<TObject>(bytes);
    }

    private sealed class TestMessage
    {
        public int Value { get; set; }
    }

    private sealed class ConflictConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message)
            => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex)
            => Task.FromResult(ConsumerState.Ack);
    }
}
