using Maomi.MQ;
using Maomi.MQ.Consumer;
using Maomi.MQ.Default;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Hosts;
using Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Hosts;

public class MessageConsumerTests
{
    [Fact]
    public async Task ConsumerAsync_WhenConsumerInstanceIsMissing_ShouldNotifyBreakdownAndThrow()
    {
        var breakdown = new Mock<IBreakdown>(MockBehavior.Strict);
        breakdown
            .Setup(x => x.NotFoundConsumerAsync("queue-a", typeof(TestMessage), typeof(IConsumer<TestMessage>)))
            .Returns(Task.CompletedTask);

        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")],
            breakdown: breakdown.Object);

        var channel = CreateAckNackChannel();
        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a"),
            _ => new object());

        var eventArgs = CreateEventArgs("application/json", new TestMessage { Value = 1 });

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => consumer.ConsumerAsync(channel.Object, eventArgs));

        Assert.Equal("consumer", ex.ParamName);
        breakdown.VerifyAll();
        channel.Verify(
            x => x.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        channel.Verify(
            x => x.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConsumerAsync_WhenFallbackReturnsAck_ShouldAckMessage()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var consumerInstance = new DelegateConsumer(
            execute: (_, _) => Task.CompletedTask,
            faild: (_, _, _, _) => Task.CompletedTask,
            fallback: (_, _, _) => Task.FromResult(ConsumerState.Ack));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a", retryFaildRequeue: false),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "application/xml", body: new TestMessage { Value = 1 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        channel.Verify(
            x => x.BasicAckAsync(eventArgs.DeliveryTag, false, It.IsAny<CancellationToken>()),
            Times.Once);
        channel.Verify(
            x => x.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConsumerAsync_WhenFallbackReturnsNack_ShouldUseRetryFaildRequeueFlag()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var consumerInstance = new DelegateConsumer(
            execute: (_, _) => Task.CompletedTask,
            faild: (_, _, _, _) => Task.CompletedTask,
            fallback: (_, _, _) => Task.FromResult(ConsumerState.Nack));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a", retryFaildRequeue: true),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "unsupported/type", body: new TestMessage { Value = 2 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        channel.Verify(
            x => x.BasicNackAsync(eventArgs.DeliveryTag, false, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumerAsync_WhenFallbackReturnsNackAndNoRequeue_ShouldNackWithoutRequeue()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var consumerInstance = new DelegateConsumer(
            execute: (_, _) => Task.CompletedTask,
            faild: (_, _, _, _) => Task.CompletedTask,
            fallback: (_, _, _) => Task.FromResult(ConsumerState.NackAndNoRequeue));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a", retryFaildRequeue: true),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "unsupported/type", body: new TestMessage { Value = 3 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        channel.Verify(
            x => x.BasicNackAsync(eventArgs.DeliveryTag, false, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumerAsync_WhenExecuteFailsThenRetrySucceeds_ShouldAckAndCallFaildOnce()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(1),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var executeCount = 0;
        var faildCount = 0;

        var consumerInstance = new DelegateConsumer(
            execute: (_, _) =>
            {
                executeCount++;
                if (executeCount == 1)
                {
                    throw new InvalidOperationException("first execute failed");
                }

                return Task.CompletedTask;
            },
            faild: (_, _, _, _) =>
            {
                faildCount++;
                return Task.CompletedTask;
            },
            fallback: (_, _, _) => Task.FromResult(ConsumerState.Ack));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a"),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "application/json", body: new TestMessage { Value = 4 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        Assert.Equal(2, executeCount);
        Assert.Equal(1, faildCount);
        channel.Verify(
            x => x.BasicAckAsync(eventArgs.DeliveryTag, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumerAsync_WhenFaildThrows_ShouldFallbackAndNack()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var consumerInstance = new DelegateConsumer(
            execute: (_, _) => throw new InvalidOperationException("execute failed"),
            faild: (_, _, _, _) => throw new InvalidOperationException("faild failed"),
            fallback: (_, _, _) => Task.FromResult(ConsumerState.NackAndRequeue));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a", retryFaildRequeue: false),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "application/json", body: new TestMessage { Value = 5 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        channel.Verify(
            x => x.BasicNackAsync(eventArgs.DeliveryTag, false, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumerAsync_WhenFallbackThrows_ShouldUseDefaultNackBranch()
    {
        var provider = CreateServiceProvider(
            retryFactory: new FixedRetryPolicyFactory(0),
            messageSerializers: [new FakeMessageSerializer("application/json")]);

        var channel = CreateAckNackChannel();
        var consumerInstance = new DelegateConsumer(
            execute: (_, _) => Task.CompletedTask,
            faild: (_, _, _, _) => Task.CompletedTask,
            fallback: (_, _, _) => throw new InvalidOperationException("fallback failed"));

        var consumer = new MessageConsumer<TestMessage>(
            provider,
            CreateOptions("queue-a", retryFaildRequeue: false),
            _ => consumerInstance);

        var eventArgs = CreateEventArgs(contentType: "unknown/format", body: new TestMessage { Value = 6 });

        await consumer.ConsumerAsync(channel.Object, eventArgs);

        channel.Verify(
            x => x.BasicNackAsync(eventArgs.DeliveryTag, false, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static IServiceProvider CreateServiceProvider(
        IRetryPolicyFactory retryFactory,
        IReadOnlyCollection<IMessageSerializer> messageSerializers,
        IBreakdown? breakdown = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new MqOptions
        {
            AppName = "ut-app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = new Mock<IConnectionFactory>().Object,
            MessageSerializers = messageSerializers,
        });
        services.AddSingleton(retryFactory);
        services.AddSingleton<IIdProvider>(new DefaultIdProvider(1));
        services.AddSingleton(Mock.Of<IConsumerDiagnostics>());
        services.AddSingleton<IBreakdown>(breakdown ?? Mock.Of<IBreakdown>());
        services.AddSingleton<ServiceFactory>();
        return services.BuildServiceProvider();
    }

    private static ConsumerOptions CreateOptions(string queue, bool retryFaildRequeue = false)
    {
        return new ConsumerOptions
        {
            Queue = queue,
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            RetryFaildRequeue = retryFaildRequeue,
            Qos = 5,
        };
    }

    private static Mock<IChannel> CreateAckNackChannel()
    {
        var channel = new Mock<IChannel>(MockBehavior.Strict);
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

        return channel;
    }

    private static BasicDeliverEventArgs CreateEventArgs(string contentType, TestMessage body)
    {
        var properties = new BasicProperties
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            ContentType = contentType,
            Type = nameof(TestMessage),
            AppId = "ut-app",
        };

        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body);
        return new BasicDeliverEventArgs(
            "ctag",
            1,
            false,
            "ex",
            "route",
            properties,
            payload,
            CancellationToken.None);
    }

    private sealed class FixedRetryPolicyFactory : IRetryPolicyFactory
    {
        private readonly AsyncRetryPolicy _policy;

        public FixedRetryPolicyFactory(int retryCount)
        {
            _policy = Policy
                .Handle<Exception>()
                .RetryAsync(retryCount);
        }

        public Task<AsyncRetryPolicy> CreatePolicy(string queue, string id)
        {
            return Task.FromResult(_policy);
        }
    }

    private sealed class DelegateConsumer : IConsumer<TestMessage>
    {
        private readonly Func<MessageHeader, TestMessage, Task> _execute;
        private readonly Func<MessageHeader, Exception, int, TestMessage, Task> _faild;
        private readonly Func<MessageHeader, TestMessage?, Exception?, Task<ConsumerState>> _fallback;

        public DelegateConsumer(
            Func<MessageHeader, TestMessage, Task> execute,
            Func<MessageHeader, Exception, int, TestMessage, Task> faild,
            Func<MessageHeader, TestMessage?, Exception?, Task<ConsumerState>> fallback)
        {
            _execute = execute;
            _faild = faild;
            _fallback = fallback;
        }

        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message)
            => _execute(messageHeader, message);

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message)
            => _faild(messageHeader, ex, retryCount, message);

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex)
            => _fallback(messageHeader, message, ex);
    }

    private sealed class TestMessage
    {
        public int Value { get; set; }
    }
}
