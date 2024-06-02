using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;
using System.Reflection;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class EventGroupConsumerHostTests
{
    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    private readonly Mock<IConnection> _mockConnection = new Mock<IConnection>();
    private readonly Mock<IChannel> _mockChannel = new Mock<IChannel>();
    public EventGroupConsumerHostTests()
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

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_Test1Event));
        typeFilter.Filter(services, typeof(Group_Test2Event));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test2Event>));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();

        var eventInfo = ioc.GetKeyedService<EventGroupInfo>("group");
        Assert.NotNull(eventInfo);
        Assert.Equal("group", eventInfo.Group);
        Assert.Equal(2, eventInfo.EventInfos.Count);
        Assert.Equal(10, eventInfo.EventInfos["test1"].Qos);

        using var hostService = new TestDefaultConsumerHostService(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<EventGroupConsumerHostService>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory,
            eventInfo
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

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_Test1Event));
        typeFilter.Filter(services, typeof(Group_Test2Event));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test2Event>));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();

        var evenGrouptInfo = ioc.GetKeyedService<EventGroupInfo>("group");
        Assert.NotNull(evenGrouptInfo);
        Assert.Equal("group", evenGrouptInfo.Group);
        Assert.Equal(2, evenGrouptInfo.EventInfos.Count);
        Assert.Equal(10, evenGrouptInfo.EventInfos["test1"].Qos);

        var eventInfo = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == "test1").Value;
        Assert.NotNull(eventInfo);

        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Throws(new SocketException(400, "error"));

        using var hostService = new TestDefaultConsumerHostService(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<EventGroupConsumerHostService>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory,
            evenGrouptInfo
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
    public async Task ConsumerAsync()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();
        var jsonSerializer = new DefaultJsonSerializer();

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_Test1Event));
        typeFilter.Filter(services, typeof(Group_Test2Event));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test2Event>));
        typeFilter.Build(services);

        services.Add(new ServiceDescriptor(serviceKey: "test1",
            serviceType: typeof(IConsumer<Group_Test1Event>),
            implementationType: typeof(EmptyConsumer<Group_Test1Event>),
            lifetime: ServiceLifetime.Singleton));

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var ioc = services.BuildServiceProvider();

        var consumer = ioc.GetRequiredKeyedService<IConsumer<Group_Test1Event>>("test1") as EmptyConsumer<Group_Test1Event>;
        Assert.NotNull(consumer);

        var evenGrouptInfo = ioc.GetKeyedService<EventGroupInfo>("group");
        Assert.NotNull(evenGrouptInfo);

        var eventInfo = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == "test1").Value;
        Assert.NotNull(eventInfo);

        using var hostService = new TestDefaultConsumerHostService(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<EventGroupConsumerHostService>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory,
            evenGrouptInfo
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody = new EventBody<Group_Test1Event>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new Group_Test1Event
            {
                Id = 1
            }
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync<Group_Test1Event>(eventInfo, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        Assert.Equal(eventBody.Id, consumer.EventBody.Id);
        Assert.Equal(eventBody.CreateTime, consumer.EventBody.CreateTime);
        Assert.Equal(eventBody.Body.Id, consumer.EventBody.Body.Id);
    }

    [Fact]
    public async Task Two_Event_Group()
    {
        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();
        var jsonSerializer = new DefaultJsonSerializer();

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_Test1Event));
        typeFilter.Filter(services, typeof(Group_Test2Event));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test1Event>));
        typeFilter.Filter(services, typeof(TEventEventHandler<Group_Test2Event>));
        typeFilter.Build(services);

        services.Add(new ServiceDescriptor(serviceKey: "test1",
            serviceType: typeof(IConsumer<Group_Test1Event>),
            implementationType: typeof(EmptyConsumer<Group_Test1Event>),
            lifetime: ServiceLifetime.Singleton));

        services.Add(new ServiceDescriptor(serviceKey: "test2",
            serviceType: typeof(IConsumer<Group_Test2Event>),
            implementationType: typeof(EmptyConsumer<Group_Test2Event>),
            lifetime: ServiceLifetime.Singleton));

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var ioc = services.BuildServiceProvider();

        var consumer1 = ioc.GetRequiredKeyedService<IConsumer<Group_Test1Event>>("test1") as EmptyConsumer<Group_Test1Event>;
        var consumer2 = ioc.GetRequiredKeyedService<IConsumer<Group_Test2Event>>("test2") as EmptyConsumer<Group_Test2Event>;

        Assert.NotNull(consumer1);
        Assert.NotNull(consumer2);

        var evenGrouptInfo = ioc.GetKeyedService<EventGroupInfo>("group");
        Assert.NotNull(evenGrouptInfo);

        var eventInfo1 = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == "test1").Value;
        var eventInfo2 = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == "test2").Value;

        Assert.NotNull(eventInfo1);
        Assert.NotNull(eventInfo2);

        using var hostService = new TestDefaultConsumerHostService(
            ioc,
            new DefaultMqOptions
            {
                ConnectionFactory = _mockConnectionFactory.Object
            },
            new DefaultJsonSerializer(),
            LoggerHeler.Create<EventGroupConsumerHostService>(),
            new DefaultRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>()),
            waitReadyFactory,
            evenGrouptInfo
            );

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody1 = new EventBody<Group_Test1Event>()
        {
            Id = 1,
            Queue = "test1",
            CreateTime = DateTimeOffset.Now,
            Body = new Group_Test1Event
            {
                Id = 1
            }
        };
        var eventBody2 = new EventBody<Group_Test2Event>()
        {
            Id = 1,
            Queue = "test2",
            CreateTime = DateTimeOffset.Now,
            Body = new Group_Test2Event
            {
                Id = 1
            }
        };
        var bytes1 = jsonSerializer.Serializer(eventBody1);
        var bytes2 = jsonSerializer.Serializer(eventBody2);

        var buffer1 = new byte[1000];
        var buffer2 = new byte[1000];

        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        _basicProperties.WriteTo(buffer1);
        _basicProperties.WriteTo(buffer2);

        await hostService.PublishAsync<Group_Test1Event>(eventInfo1, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "", "test1", new ReadOnlyBasicProperties(buffer1.AsSpan()), bytes1));
        await hostService.PublishAsync<Group_Test2Event>(eventInfo2, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "", "test2", new ReadOnlyBasicProperties(buffer2.AsSpan()), bytes2));

        Assert.Equal(eventBody1.Id, consumer1.EventBody.Id);
        Assert.Equal(eventBody1.CreateTime, consumer1.EventBody.CreateTime);
        Assert.Equal(eventBody1.Body.Id, consumer1.EventBody.Body.Id);

        Assert.Equal(eventBody2.Id, consumer2.EventBody.Id);
        Assert.Equal(eventBody2.CreateTime, consumer2.EventBody.CreateTime);
        Assert.Equal(eventBody2.Body.Id, consumer2.EventBody.Body.Id);
    }

    public async Task<TConsumer> Retry<TEvent, TConsumer, TEventHandler>(string queue, Action<IServiceCollection>? action = null)
    where TConsumer : class, IConsumer<TEvent>, IRetry
    where TEvent : class, new()
    {
        var eventTopicAttribute = typeof(TEvent).GetCustomAttribute<EventTopicAttribute>();
        Assert.NotNull(eventTopicAttribute);

        var services = new ServiceCollection();
        var waitReadyFactory = new DefaultWaitReadyFactory();
        var jsonSerializer = new DefaultJsonSerializer();
        var retryFactory = new TestRetryPolicyFactory(LoggerHeler.Create<DefaultRetryPolicyFactory>());

        services.AddSingleton<IWaitReadyFactory>(waitReadyFactory);
        services.AddSingleton<IJsonSerializer>(jsonSerializer);
        services.AddSingleton<IRetryPolicyFactory>(retryFactory);
        services.AddSingleton<DefaultMqOptions>(s => new DefaultMqOptions
        {
            ConnectionFactory = _mockConnectionFactory.Object
        });

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(TEvent));
        typeFilter.Filter(services, typeof(TEventHandler));
        typeFilter.Build(services);

        services.Add(new ServiceDescriptor(serviceKey: queue,
            serviceType: typeof(IConsumer<TEvent>),
            implementationType: typeof(TConsumer),
            lifetime: ServiceLifetime.Singleton));

        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        if (action != null)
        {
            action.Invoke(services);
        }

        services.AddSingleton<TestDefaultConsumerHostService>(s =>
        {
            return new TestDefaultConsumerHostService(
                s,
                s.GetRequiredService<DefaultMqOptions>(),
                s.GetRequiredService<IJsonSerializer>(),
                s.GetRequiredService<ILogger<EventGroupConsumerHostService>>(),
                s.GetRequiredService<IRetryPolicyFactory>(),
                s.GetRequiredService<IWaitReadyFactory>(),
                s.GetKeyedService<EventGroupInfo>("group")!);
        });

        var ioc = services.BuildServiceProvider();

        var evenGrouptInfo = ioc.GetKeyedService<EventGroupInfo>("group");
        Assert.NotNull(evenGrouptInfo);

        var eventInfo = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == eventTopicAttribute.Queue).Value;
        Assert.NotNull(eventInfo);

        var consumer = ioc.GetRequiredKeyedService<IConsumer<TEvent>>(queue) as TConsumer;
        Assert.NotNull(consumer);
        using var hostService = ioc.GetRequiredService<TestDefaultConsumerHostService>();

        await hostService.StartAsync(CancellationToken.None);
        await waitReadyFactory.WaitReady();

        var eventBody = new EventBody<TEvent>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new TEvent()
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync<TEvent>(eventInfo, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));
        return consumer;
    }

    [Fact]
    public async Task Retry_Five_Times()
    {
        var consumer = await Retry<Group_Test1Event, ConsumerException<Group_Test1Event>, TEventEventHandler<Group_Test1Event>>("test1");

        // Run once and retry five times
        Assert.Equal(6, consumer.RetryCount);
        Assert.True(consumer.IsFallbacked);
    }

    [Fact]
    public async Task Retry_Faild_And_Fallback_False_Requeue()
    {
        await Retry<TrueRequeueEvent_Group, Retry_Faild_FallBack_False_Consumer<TrueRequeueEvent_Group>, TEventEventHandler<TrueRequeueEvent_Group>>("test4");
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Retry_Faild_And_Fallback_True()
    {
        await Retry<TrueRequeueEvent_Group, Retry_Faild_Fallback_True_Consumer<TrueRequeueEvent_Group>, TEventEventHandler<TrueRequeueEvent_Group>>("test4");
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Exception_NoRequeue()
    {
        var consumer = await Retry<FalseRequeueEvent_Group, ConsumerException<FalseRequeueEvent_Group>, TEventEventHandler<FalseRequeueEvent_Group>>("test3", (services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Exception_Requeue()
    {
        var consumer = await Retry<TrueRequeueEvent_Group, ConsumerException<TrueRequeueEvent_Group>, TEventEventHandler<TrueRequeueEvent_Group>>("test4", (services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(-1, consumer.RetryCount);
    }
}
