using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Net.Sockets;
using System.Reflection;

namespace Maomi.MQ.Tests.CustomConsumer;
public class EventGroupConsumerHostTests
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
        typeFilter.Filter(services, typeof(Test1Event));
        typeFilter.Filter(services, typeof(Test2Event));
        typeFilter.Filter(services, typeof(My1EventEventHandler));
        typeFilter.Filter(services, typeof(My2EventEventHandler));
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
        typeFilter.Filter(services, typeof(Test1Event));
        typeFilter.Filter(services, typeof(Test2Event));
        typeFilter.Filter(services, typeof(My1EventEventHandler));
        typeFilter.Filter(services, typeof(My2EventEventHandler));
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
        typeFilter.Filter(services, typeof(Test1Event));
        typeFilter.Filter(services, typeof(Test2Event));
        typeFilter.Filter(services, typeof(My1EventEventHandler));
        typeFilter.Filter(services, typeof(My2EventEventHandler));
        typeFilter.Build(services);

        services.AddSingleton<IConsumer<Test1Event>, Consumer1>();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var ioc = services.BuildServiceProvider();

        var consumer = ioc.GetRequiredService<IConsumer<Test1Event>>() as Consumer1;
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

        var eventBody = new EventBody<Test1Event>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new Test1Event
            {
                Id = 1
            }
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync<Test1Event>(eventInfo, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

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
        typeFilter.Filter(services, typeof(Test1Event));
        typeFilter.Filter(services, typeof(Test2Event));
        typeFilter.Filter(services, typeof(My1EventEventHandler));
        typeFilter.Filter(services, typeof(My2EventEventHandler));
        typeFilter.Build(services);

        services.AddSingleton<IConsumer<Test1Event>, Consumer1>();
        services.AddSingleton<IConsumer<Test2Event>, Consumer2>();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var ioc = services.BuildServiceProvider();

        var consumer1 = ioc.GetRequiredService<IConsumer<Test1Event>>() as Consumer1;
        var consumer2 = ioc.GetRequiredService<IConsumer<Test2Event>>() as Consumer2;

        Assert.NotNull(consumer1);
        Assert.NotNull(consumer2);

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

        var eventBody1 = new EventBody<Test1Event>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new Test1Event
            {
                Id = 1
            }
        };
        var eventBody2 = new EventBody<Test2Event>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new Test2Event
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

        await hostService.PublishAsync<Test1Event>(eventInfo, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer1.AsSpan()), bytes1));
        await hostService.PublishAsync<Test2Event>(eventInfo, _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer2.AsSpan()), bytes2));

        Assert.Equal(eventBody1.Id, consumer1.EventBody.Id);
        Assert.Equal(eventBody1.CreateTime, consumer1.EventBody.CreateTime);
        Assert.Equal(eventBody1.Body.Id, consumer1.EventBody.Body.Id);

        Assert.Equal(eventBody2.Id, consumer2.EventBody.Id);
        Assert.Equal(eventBody2.CreateTime, consumer2.EventBody.CreateTime);
        Assert.Equal(eventBody2.Body.Id, consumer2.EventBody.Body.Id);
    }

    public async Task<TConsumer> Retry<TConsumer, TEvent, TEventHandler>(Action<IServiceCollection>? action = null)
    where TConsumer : class, IConsumer<TEvent>, IRetry
    where TEvent : class, new()
    {
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

        services.AddSingleton<IConsumer<TEvent>, TConsumer>();
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

        var eventInfo = evenGrouptInfo.EventInfos.FirstOrDefault(x => x.Key == "test3").Value;
        Assert.NotNull(eventInfo);

        var consumer = ioc.GetRequiredService<IConsumer<TEvent>>() as TConsumer;
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
        var consumer = await Retry<ConsumerException<Test1Event>, Test1Event, TEventEventHandler<Test1Event>>();

        Assert.Equal(6, consumer.RetryCount);
        Assert.True(consumer.IsFallbacked);
    }

    [Fact]
    public async Task RetryFaild_And_Fallback_False_Requeue()
    {
        await Retry<Consumer_FallBack_False<TrueRequeueEvent>, TrueRequeueEvent, TEventEventHandler<TrueRequeueEvent>>();
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task RetryFaild_And_Fallback_True_Requeue()
    {
        await Retry<Consumer_Fallback_True<TrueRequeueEvent>, TrueRequeueEvent, TEventEventHandler<TrueRequeueEvent>>();
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetryFaild_And_Fallback_True_NoRequeue()
    {
        await Retry<ConsumerFalseRetryFaildRequeue<FalseRequeueEvent>, FalseRequeueEvent, TEventEventHandler<TrueRequeueEvent>>();
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Exception_NoRequeue()
    {
        var consumer = await Retry<ConsumerException<FalseRequeueEvent>, FalseRequeueEvent, TEventEventHandler<FalseRequeueEvent>>((services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Exception_Requeue()
    {
        var eventBody = new EventBody<FalseRequeueEvent>()
        {
            Id = 1,
            CreateTime = DateTimeOffset.Now,
            Body = new FalseRequeueEvent
            {
                Id = 1
            }
        };

        var consumer = await Retry<ConsumerException<TrueRequeueEvent>, TrueRequeueEvent, TEventEventHandler<TrueRequeueEvent>>((services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(-1, consumer.RetryCount);
    }

    public class ExceptionJsonSerializer : IJsonSerializer
    {
        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes) where TObject : class
        {
            throw new NotImplementedException();
        }

        public byte[] Serializer<TObject>(TObject obj) where TObject : class
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }

    [EventTopic("test1", Group = "group")]
    public class Test1Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test2", Group = "group")]
    public class Test2Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test3", Group = "group", Qos = 10, RetryFaildRequeue = false, ExecptionRequeue = false)]
    public class FalseRequeueEvent
    {
        public int Id { get; set; }
    }

    [EventTopic("test4", Group = "group", Qos = 10, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class TrueRequeueEvent
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class My1EventEventHandler : IEventHandler<Test1Event>
    {
        public async Task CancelAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(0)]
    public class My2EventEventHandler : IEventHandler<Test2Event>
    {
        public async Task CancelAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(0)]
    public class TEventEventHandler<TEvent> : IEventHandler<TEvent>
    {
        public async Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
        }
    }

    public class Consumer1 : EventBusConsumer<Test1Event>
    {
        public EventBody<Test1Event> EventBody { get; private set; }
        public Consumer1(IEventMiddleware<Test1Event> eventMiddleware, HandlerMediator<Test1Event> handlerBroker, ILogger<EventBusConsumer<Test1Event>> logger, IServiceProvider serviceProvider) : base(eventMiddleware, handlerBroker, logger, serviceProvider)
        {
        }

        public override Task ExecuteAsync(EventBody<Test1Event> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
    }

    public class Consumer2 : EventBusConsumer<Test2Event>
    {
        public Consumer2(IEventMiddleware<Test2Event> eventMiddleware, HandlerMediator<Test2Event> handlerBroker, ILogger<EventBusConsumer<Test2Event>> logger, IServiceProvider serviceProvider) : base(eventMiddleware, handlerBroker, logger, serviceProvider)
        {
        }

        public EventBody<Test2Event> EventBody { get; private set; }

        public override Task ExecuteAsync(EventBody<Test2Event> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
    }

    public class ConsumerException<TEvent> : IConsumer<TEvent>, IRetry
        where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount++;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }
    public class Consumer_FallBack_False<TEvent> : IConsumer<TEvent>, IRetry
        where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount++;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = false;
            return Task.FromResult(false);
        }
    }

    public class Consumer_Fallback_True<TEvent> : IConsumer<TEvent>, IRetry
                where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount++;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(true);
        }
    }
    public class TestDefaultConsumerHostService : EventGroupConsumerHostService
    {
        public TestDefaultConsumerHostService(
            IServiceProvider serviceProvider,
            DefaultMqOptions connectionOptions,
            IJsonSerializer jsonSerializer,
            ILogger<EventGroupConsumerHostService> logger,
            IRetryPolicyFactory policyFactory,
            IWaitReadyFactory waitReadyFactory,
            EventGroupInfo eventGroupInfo) : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory, eventGroupInfo)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public async Task PublishAsync<TEvent>(EventBus.EventInfo eventInfo, IChannel channel, BasicDeliverEventArgs eventArgs)
            where TEvent : class
        {
            await ConsumerAsync<TEvent>(eventInfo, channel, eventArgs);
        }
    }
}
