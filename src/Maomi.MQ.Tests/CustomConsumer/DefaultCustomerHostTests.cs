using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;
using static Maomi.MQ.Tests.CustomConsumer.DefaultCustomerHostTests;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class DefaultCustomerHostTests
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
        typeFilter.Filter(services, typeof(EmptyConsumer<IdEvent>));
        var ioc = services.BuildServiceProvider();

        using var hostService = new TestDefaultConsumerHostService<EmptyConsumer<IdEvent>, IdEvent>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<EmptyConsumer<IdEvent>, IdEvent>>(),
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
        typeFilter.Filter(services, typeof(EmptyConsumer<IdEvent>));
        var ioc = services.BuildServiceProvider();

        using var hostService = new TestDefaultConsumerHostService<EmptyConsumer<IdEvent>, IdEvent>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<EmptyConsumer<IdEvent>, IdEvent>>(),
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
        typeFilter.Filter(services, typeof(EmptyConsumer<IdEvent>));
        var ioc = services.BuildServiceProvider();

        using var hostService = new DefaultConsumerHostService<EmptyConsumer<IdEvent>, IdEvent>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<EmptyConsumer<IdEvent>, IdEvent>>(),
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
        typeFilter.Filter(services, typeof(EmptyConsumer<IdEvent>));
        services.AddSingleton<IConsumer<IdEvent>, EmptyConsumer<IdEvent>>();
        var ioc = services.BuildServiceProvider();

        var consumer = ioc.GetRequiredService<IConsumer<IdEvent>>() as EmptyConsumer<IdEvent>;
        Assert.NotNull(consumer);
        using var hostService = new TestDefaultConsumerHostService<EmptyConsumer<IdEvent>, IdEvent>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            jsonSerializer,
            LoggerHeler.Create<ConsumerBaseHostSrvice<EmptyConsumer<IdEvent>, IdEvent>>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody = new EventBody<IdEvent>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new IdEvent
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

    public async Task<TConsumer> Retry<TConsumer, TEvent>(Action<IServiceCollection>? action = null)
        where TConsumer : class, IConsumer<TEvent>, IRetry
        where TEvent : class
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();
        var jsonSerializer = new DefaultJsonSerializer();
        var retryFactory = new TestRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>());

        services.AddSingleton<IWaitReadyFactory>(waitReadyFactory);
        services.AddSingleton<IJsonSerializer>(jsonSerializer);
        services.AddSingleton<IRetryPolicyFactory>(retryFactory);

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(TConsumer));
        services.AddSingleton<IConsumer<TEvent>, TConsumer>();
        if (action != null)
        {
            action.Invoke(services);
        }
        var ioc = services.BuildServiceProvider();

        var consumer = ioc.GetRequiredService<IConsumer<IdEvent>>() as TConsumer;
        Assert.NotNull(consumer);
        using var hostService = new TestDefaultConsumerHostService<TConsumer, TEvent>(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            ioc.GetRequiredService<IJsonSerializer>(),
            LoggerHeler.Create<ConsumerBaseHostSrvice<TConsumer, TEvent>>(),
            ioc.GetRequiredService<IRetryPolicyFactory>(),
            ioc.GetRequiredService<IWaitReadyFactory>()
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody = new EventBody<IdEvent>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new IdEvent
            {
                Id = 1
            }
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync(_mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        return consumer;
    }

    [Fact]
    public async Task Retry_Five_Times()
    {
        var consumer = await Retry<Exception_NoRequeue_Consumer<IdEvent>, IdEvent>();

        Assert.Equal(6, consumer.RetryCount);
        Assert.True(consumer.IsFallbacked);
    }

    // Retry faild,fallback false,requeue
    [Fact]
    public async Task RetryFaild_And_Fallback_False_Requeue()
    {
        await Retry<Faild_Fallback_False_Requeue_Consumer<IdEvent>, IdEvent>();
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Retry faild,fallback false
    [Fact]
    public async Task RetryFaild_And_Fallback_False_NoRequeue()
    {
        await Retry<Faild_Fallback_True_Consumer<IdEvent>, IdEvent>();
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryFaild_And_Fallback_True()
    {
        await Retry<Faild_Fallback_True_Consumer<IdEvent>, IdEvent>();
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Exception_NoRequeue()
    {
        var consumer = await Retry<Exception_NoRequeue_Consumer<IdEvent>, IdEvent>((services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Exception_Requeue()
    {
        var eventBody = new EventBody<IdEvent>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new IdEvent
            {
                Id = 1
            }
        };

        var consumer = await Retry<Exception_Requeue_Consumer<IdEvent>, IdEvent>((services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(-1, consumer.RetryCount);
    }

}
