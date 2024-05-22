using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;

namespace Maomi.MQ.Tests.CustomConsumer;

public class DefaultCustomerHostTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IConnection> _mockConnection = new Mock<IConnection>();
    private readonly Mock<IChannel> _mockChannel = new Mock<IChannel>();
    public DefaultCustomerHostTests()
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

    [Fact]
    public async Task WaitReady()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer1));
        var ioc = services.BuildServiceProvider();

        using var hostService = new TestDefaultConsumerHostService<Consumer1, TestEvent1>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<Consumer1, TestEvent1>>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory
            );


        await hostService.StartAsync(CancellationToken.None);
        var task = waitReadyFactory.WaitReady();
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task WaitReady_Exception()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();

        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Throws(new SocketException(400, "error"));

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer1));
        var ioc = services.BuildServiceProvider();

        using var hostService = new TestDefaultConsumerHostService<Consumer1, TestEvent1>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<Consumer1, TestEvent1>>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory
            );

        try
        {
            await hostService.StartAsync(CancellationToken.None);
        }
        catch { }
        var task = waitReadyFactory.WaitReady();
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.IsType<SocketException>(task.Exception?.InnerExceptions?.FirstOrDefault());
        await Assert.ThrowsAsync<SocketException>(async () => await task);
    }

    [Fact]
    public async Task ExecuteAsync()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer1));
        var ioc = services.BuildServiceProvider();

        using var hostService = new DefaultConsumerHostService<Consumer1, TestEvent1>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<Consumer1, TestEvent1>>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();
    }

    [Fact]
    public async Task ConsumerAsync()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();
        var jsonSerializer = new DefaultJsonSerializer();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer1));
        services.AddSingleton<IConsumer<TestEvent1>, Consumer1>();
        var ioc = services.BuildServiceProvider();

        var consumer = ioc.GetRequiredService<IConsumer<TestEvent1>>() as Consumer1;
        Assert.NotNull(consumer);
        using var hostService = new TestDefaultConsumerHostService<Consumer1, TestEvent1>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            jsonSerializer,
            LoggerHeler.Create<ConsumerBaseHostSrvice<Consumer1, TestEvent1>>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody = new EventBody<TestEvent1>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new TestEvent1
            {
                Id = 1
            }
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync(_mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        Assert.Equal(eventBody.Id, consumer.EventBody.Id);
        Assert.Equal(eventBody.CreateTime, consumer.EventBody.CreateTime);
        Assert.Equal(eventBody.Body.Id, consumer.EventBody.Body.Id);
    }

    // 测试各种重试策略

    public class TestEvent1
    {
        public int Id { get; set; }
    }

    [Consumer("tes", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Consumer1 : IConsumer<TestEvent1>
    {
        public EventBody<TestEvent1> EventBody { get;private set; }

        public Task ExecuteAsync(EventBody<TestEvent1> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent1>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent1>? message) => Task.FromResult(true);
    }

    public class TestDefaultConsumerHostService<TConsumer, TEvent> : DefaultConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public TestDefaultConsumerHostService(IServiceProvider serviceProvider, DefaultMqOptions connectionOptions, IJsonSerializer jsonSerializer, ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger, IRetryPolicyFactory policyFactory, IWaitReadyFactory waitReadyFactory) : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        // Analog received data.
        public async Task PublishAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
        {
            await base.ConsumerAsync(channel, eventArgs);
        }
    }
}
