using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Hosts;

public class ConsumerHostedServiceTests
{
    [Fact]
    public async Task WaitReadyInitQueueAsync_ShouldInitializeAndCreateConsumerChannel()
    {
        var hostLifetime = new Mock<IHostApplicationLifetime>(MockBehavior.Strict);
        var channel = new Mock<IChannel>(MockBehavior.Strict);
        var consumeChannel = new Mock<IChannel>(MockBehavior.Strict);
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        var pool = new Mock<ConnectionPool>(CreateOptions());

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
            .ReturnsAsync(new QueueDeclareOk("queue", 0, 0));

        channel
            .SetupAdd(x => x.BasicReturnAsync += It.IsAny<AsyncEventHandler<BasicReturnEventArgs>>());

        consumeChannel
            .Setup(x => x.BasicQosAsync(0, 10, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        consumeChannel
            .Setup(x => x.BasicConsumeAsync(
                "queue-a",
                false,
                It.IsAny<string>(),
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("ctag-1");

        consumeChannel.SetupAdd(x => x.BasicReturnAsync += It.IsAny<AsyncEventHandler<BasicReturnEventArgs>>());

        connection
            .Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consumeChannel.Object);

        var connObj = new Mock<IConnectionObject>(MockBehavior.Strict);
        connObj.SetupGet(x => x.DefaultChannel).Returns(channel.Object);
        connObj.SetupGet(x => x.Connection).Returns(connection.Object);

        pool.Setup(x => x.Get()).Returns(connObj.Object);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateOptions());
        services.AddSingleton<IRetryPolicyFactory>(new Mock<IRetryPolicyFactory>().Object);
        services.AddSingleton<IIdProvider>(new DefaultIdProvider(1));
        services.AddSingleton<IConsumerDiagnostics>(new Mock<IConsumerDiagnostics>().Object);
        services.AddSingleton<IRoutingProvider, Maomi.MQ.Models.RoutingProvider>();
        services.AddScoped<IBreakdown, DefaultBreakdown>();
        services.AddScoped<ServiceFactory>();
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<ServiceFactory>();
        var consumerTypes = new List<ConsumerType>
        {
            new()
            {
                Queue = "queue-a",
                Consumer = typeof(TestConsumer),
                Event = typeof(TestMessage),
                ConsumerOptions = new ConsumerOptions
                {
                    Queue = "queue-a",
                    AutoQueueDeclare = AutoQueueDeclare.Enable,
                    Qos = 10,
                }
            }
        };

        var service = new TestConsumerHostedService(hostLifetime.Object, factory, pool.Object, consumerTypes);

        await service.InvokeWaitReadyInitQueueAsync();

        channel.Verify(x => x.QueueDeclareAsync(
            "queue-a",
            true,
            false,
            false,
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);

        consumeChannel.Verify(x => x.BasicConsumeAsync(
            "queue-a",
            false,
            It.IsAny<string>(),
            false,
            false,
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<IAsyncBasicConsumer>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static MqOptions CreateOptions()
    {
        return new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object,
            MessageSerializers =
            [
                new Maomi.MQ.RabbitMQ.UnitTests.TestDoubles.FakeMessageSerializer("application/json")
            ]
        };
    }

    [Consumer("queue-a")]
    private sealed class TestConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
    }

    private sealed class TestMessage
    {
    }

    private sealed class TestConsumerHostedService : ConsumerHostedService
    {
        public TestConsumerHostedService(
            IHostApplicationLifetime hostApplicationLifetime,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            IReadOnlyList<ConsumerType> consumerTypes)
            : base(hostApplicationLifetime, serviceFactory, connectionPool, consumerTypes)
        {
        }

        public Task InvokeWaitReadyInitQueueAsync()
        {
            return WaitReadyInitQueueAsync();
        }
    }
}
