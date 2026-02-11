using Maomi.MQ;
using ConsumerAttribute = Maomi.MQ.Attributes.ConsumerAttribute;
using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Hosts;

public class ConsumerBaseServiceTests
{
    [Fact]
    public async Task InitQueueAsync_AutoQueueDeclareDisabled_ShouldSkipDeclare()
    {
        var service = CreateService();
        var channel = new Mock<IChannel>(MockBehavior.Strict);
        var options = new ConsumerOptions
        {
            Queue = "queue-a",
            AutoQueueDeclare = AutoQueueDeclare.Disable,
        };

        await service.InvokeInitQueueAsync(channel.Object, options);
    }

    [Fact]
    public async Task InitQueueAsync_NonBroadcast_ShouldDeclareDurableQueueAndBind()
    {
        var service = CreateService();
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        channel
            .Setup(x => x.QueueDeclareAsync(
                "queue-a",
                true,
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueDeclareOk("queue-a", 0, 0));

        channel
            .Setup(x => x.ExchangeDeclareAsync(
                "bind-ex",
                "direct",
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel
            .Setup(x => x.QueueBindAsync(
                "queue-a",
                "bind-ex",
                "route-a",
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = new ConsumerOptions
        {
            Queue = "queue-a",
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            BindExchange = "bind-ex",
            ExchangeType = ExchangeType.Direct,
            RoutingKey = "route-a",
        };

        await service.InvokeInitQueueAsync(channel.Object, options);
        channel.VerifyAll();
    }

    [Fact]
    public async Task InitQueueAsync_Broadcast_ShouldDeclareExclusiveQueue()
    {
        var service = CreateService();
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        channel
            .Setup(x => x.QueueDeclareAsync(
                "queue-a",
                false,
                true,
                true,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueueDeclareOk("queue-a", 0, 0));

        var options = new ConsumerOptions
        {
            Queue = "queue-a",
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            IsBroadcast = true,
        };

        await service.InvokeInitQueueAsync(channel.Object, options);
        channel.VerifyAll();
    }

    [Fact]
    public async Task BuildCreateConsumerHandler_ShouldCreateConsumerByMessageType()
    {
        var service = CreateService();
        var channel = new Mock<IChannel>(MockBehavior.Strict);

        channel
            .Setup(x => x.BasicQosAsync(0, 10, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        channel
            .Setup(x => x.BasicConsumeAsync(
                It.IsAny<string>(),
                false,
                It.IsAny<string>(),
                false,
                false,
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<IAsyncBasicConsumer>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("ctag-1");

        channel
            .SetupAdd(x => x.BasicReturnAsync += It.IsAny<AsyncEventHandler<BasicReturnEventArgs>>());

        var options = new ConsumerOptions
        {
            Queue = "queue-a",
            Qos = 10,
        };

        var handler = (Delegate)service.InvokeBuildCreateConsumerHandler(typeof(TestMessage));
        var task = (Task<string>)handler.DynamicInvoke(service, channel.Object, typeof(TestConsumer), typeof(TestMessage), options)!;
        var tag = await task;

        Assert.Equal("ctag-1", tag);
    }

    private static TestConsumerBaseService CreateService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        });

        var retryFactory = new Mock<IRetryPolicyFactory>().Object;
        var idProvider = new Mock<IIdProvider>();
        idProvider.Setup(x => x.NextId()).Returns(1);

        var diagnostics = new Mock<IConsumerDiagnostics>().Object;

        services.AddSingleton<IRetryPolicyFactory>(retryFactory);
        services.AddSingleton<IIdProvider>(idProvider.Object);
        services.AddSingleton(diagnostics);
        services.AddScoped<ServiceFactory>();
        services.AddScoped<IBreakdown, DefaultBreakdown>();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ServiceFactory>();
        return new TestConsumerBaseService(factory, provider.GetRequiredService<ILoggerFactory>());
    }

    private sealed class TestConsumerBaseService : ConsumerBaseService
    {
        public TestConsumerBaseService(ServiceFactory serviceFactory, ILoggerFactory loggerFactory)
            : base(serviceFactory, loggerFactory)
        {
        }

        public Task InvokeInitQueueAsync(IChannel channel, IConsumerOptions options)
        {
            return InitQueueAsync(channel, options);
        }

        public object InvokeBuildCreateConsumerHandler(Type messageType)
        {
            return BuildCreateConsumerHandler(messageType);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
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
}
