using Maomi.MQ.Default;
using Maomi.MQ.EventBus;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;
using System.Reflection;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class EventBusConsumerHostTests : BaseHostTests
{
    [Fact]
    public async Task WaitReady()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter = new EventBusTypeFilter();
                typeFilter.Filter(services, typeof(WaitReady_1_Event));
                typeFilter.Filter(services, typeof(WaitReady_2_Event));
                typeFilter.Filter(services, typeof(WaitReady_3_Event));
                typeFilter.Filter(services, typeof(WaitReady_1_EventHandler));
                typeFilter.Filter(services, typeof(WaitReady_2_EventHandler));
                typeFilter.Filter(services, typeof(WaitReady_3_EventHandler));
                typeFilter.Build(services);

                services.RemoveAll<IHostedService>();

                Func<IServiceProvider, WaitReady_0_ConsumerHostService> funcFactory0 = (serviceProvider) =>
                {
                    return new WaitReady_0_ConsumerHostService(
                        serviceProvider,
                        serviceProvider.GetRequiredService<ServiceFactory>(),
                        serviceProvider.GetRequiredService<ConnectionPool>(),
                        serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                        new List<ConsumerType>());
                };

                Func<IServiceProvider, WaitReady_1_ConsumerHostService> funcFactory1 = (serviceProvider) =>
                {
                    return new WaitReady_1_ConsumerHostService(
                        serviceProvider,
                        serviceProvider.GetRequiredService<ServiceFactory>(),
                        serviceProvider.GetRequiredService<ConnectionPool>(),
                        serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                        new List<ConsumerType>());
                };

                Func<IServiceProvider, WaitReady_2_ConsumerHostService> funcFactory2 = (serviceProvider) =>
                {
                    return new WaitReady_2_ConsumerHostService(
                        serviceProvider,
                        serviceProvider.GetRequiredService<ServiceFactory>(),
                        serviceProvider.GetRequiredService<ConnectionPool>(),
                        serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                        new List<ConsumerType>());
                };

                services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory0, lifetime: ServiceLifetime.Singleton));
                services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory1, lifetime: ServiceLifetime.Singleton));
                services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory2, lifetime: ServiceLifetime.Singleton));

                services.AddSingleton(_mockConnectionFactory.Object);

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                    options.Rabbit = rabbit => { };
                }, Array.Empty<Assembly>());

                services.AddSingleton<MqOptions>(new MqOptions
                {
                    AppName = "test",
                    WorkId = 0,
                    ConnectionFactory = _mockConnectionFactory.Object
                });
            }).Build();

        var waitReadyFactory = host.Services.GetRequiredService<IWaitReadyFactory>();

        var ss = host.Services.GetRequiredService<IEnumerable<IHostedService>>();
        var s0 = ss.OfType<WaitReady_0_ConsumerHostService>().First();
        var s1 = ss.OfType<WaitReady_1_ConsumerHostService>().First();
        var s2 = ss.OfType<WaitReady_2_ConsumerHostService>().First();

        _ = host.RunAsync();

        var task = waitReadyFactory.WaitReadyAsync();
        await task;
        Assert.True(task.IsCompleted);

        Assert.True(s2.InitTime > s1.InitTime);
        Assert.True(s1.InitTime > s0.InitTime);
    }

    [Fact]
    public async Task WaitReady_Exception()
    {
        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Throws(new SocketException(400, "error"));

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter = new EventBusTypeFilter();
                typeFilter.Filter(services, typeof(WaitReady_1_Event));
                typeFilter.Filter(services, typeof(WaitReady_1_EventHandler));
                typeFilter.Build(services);

                services.RemoveAll<IHostedService>();
                RegisterHost(services,new ConsumerType
                {
                    Consumer = typeof(EventBusConsumer<WaitReady_1_Event>),
                    Event = typeof(WaitReady_1_Event),
                    Queue = "test1"
                });
                services.AddSingleton(_mockConnectionFactory.Object);

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                    options.Rabbit = rabbit => { };
                }, Array.Empty<Assembly>());

                services.AddSingleton<MqOptions>(new MqOptions
                {
                    AppName = "test",
                    WorkId = 0,
                    ConnectionFactory = _mockConnectionFactory.Object
                });
            }).Build();

        var waitReadyFactory = host.Services.GetRequiredService<IWaitReadyFactory>();
        _ = host.RunAsync();

        var task = waitReadyFactory.WaitReadyAsync();
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Assert.True(ex is SocketException);
        }
        Assert.Equal(TaskStatus.Faulted, task.Status);
        Assert.IsType<SocketException>(task.Exception?.InnerExceptions?.FirstOrDefault());
        await Assert.ThrowsAsync<SocketException>(async () => await task);
    }

    [Fact]
    public async Task ExecuteAsync()
    {
        ServiceCollection services = Mock();

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(EventBusConsumer<IdEvent>));
        typeFilter.Filter(services, typeof(EventBusConsumer<IdEvent>));
        typeFilter.Filter(services, typeof(IdEvent));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var waitReadyFactory = ioc.GetRequiredService<IWaitReadyFactory>();
        var hostService = ioc.GetRequiredService<IHostedService>();

        await hostService.StartAsync(CancellationToken.None);
        var task = waitReadyFactory.WaitReadyAsync();
        await task;
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task QueueDeclare()
    {
        // get queue declare arguments.
        IDictionary<string, object> arguments = null!;
        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, bool, bool, IDictionary<string, object>, bool, bool, CancellationToken>((a1, a2, a3, a4, a5, a6, a7, a8) =>
            {
                arguments = a5;
            });

        ServiceCollection services = Mock();

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(AllOptionsEvent));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var host = ioc.GetRequiredService<IEnumerable<IHostedService>>().OfType<EventBusHostService>().First();
        _ = host.StartAsync(CancellationToken.None);

        // check arguments.
        _mockChannel.Verify(a => a.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(1000, arguments["x-expires"]);
        Assert.Equal(string.Empty, arguments["x-dead-letter-exchange"]);
        Assert.Equal("test_dead", arguments["x-dead-letter-routing-key"]);
    }

    [Fact]
    public async Task DynamicEventBusOptions()
    {
        ServiceCollection services = Mock();

        var typeFilter = new EventBusTypeFilter((options, type) =>
        {
            if (type == typeof(AllOptionsEvent))
            {
                options.Queue = options.Queue + "_1";
            }
            return true;
        });

        typeFilter.Filter(services, typeof(AllOptionsEvent));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var host = ioc.GetRequiredService<IEnumerable<IHostedService>>().OfType<EventBusHostService>().First();
        _ = host.StartAsync(CancellationToken.None);

        var consumerOptions = ioc.GetRequiredKeyedService<IConsumerOptions>("test_1");
        Assert.Equal("test_1", consumerOptions.Queue);
    }

    [Fact]
    public async Task ConsumerAsync()
    {
        ServiceCollection services = Mock();

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(IdEvent));
        typeFilter.Build(services);

        // replace default consumer.
        services.Add(new ServiceDescriptor(serviceKey: "test", serviceType: typeof(IConsumer<IdEvent>), implementationType: typeof(UnSetConsumer<IdEvent>), lifetime: ServiceLifetime.Singleton));

        RegisterHost(services, new ConsumerType
        {
            Consumer = typeof(UnSetConsumer<IdEvent>),
            Event = typeof(IdEvent),
            Queue = "test"
        });

        var ioc = services.BuildServiceProvider();

        var jsonSerializer = ioc.GetRequiredService<IJsonSerializer>();
        var consumer = ioc.GetRequiredKeyedService<IConsumer<IdEvent>>("test") as UnSetConsumer<IdEvent>;
        Assert.NotNull(consumer);
        using var hostService = ioc.GetRequiredService<IEnumerable<IHostedService>>().OfType<TestDefaultConsumerHostService>().First();

        await hostService.StartAsync(CancellationToken.None);

        var eventBody = new EventBody<IdEvent>()
        {
            Id = 1,
            CreationTime = DateTimeOffset.Now,
            Body = new IdEvent
            {
                Id = 1
            }
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync<IdEvent>("test", _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        Assert.Equal(eventBody.Id, consumer.EventBody.Id);
        Assert.Equal(eventBody.Queue, consumer.EventBody.Queue);
        Assert.Equal(eventBody.CreationTime, consumer.EventBody.CreationTime);
        Assert.Equal(eventBody.Body.Id, consumer.EventBody.Body.Id);
    }

    [Fact]
    public async Task Retry_Five_Times()
    {
        var consumer = await Retry<ConsumerException<IdEvent>, IdEvent>(new IdEvent { Id = 1 });

        // Run once and retry five times
        Assert.Equal(6, consumer.RetryCount);
        Assert.True(consumer.IsFallbacked);
    }

    [Fact]
    public async Task Retry_Faild_And_Fallback_False_Requeue()
    {
        await Retry<Retry_Faild_FallBack_False_Consumer<TrueRequeueEvent_Group>, TrueRequeueEvent_Group>(new TrueRequeueEvent_Group { Id = 1 });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Retry_Faild_And_Fallback_True()
    {
        await Retry<Retry_Faild_Fallback_True_Consumer<TrueRequeueEvent_Group>, TrueRequeueEvent_Group>(new TrueRequeueEvent_Group { Id = 1 });
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task Exception_NoRequeue()
    {
        var consumer = await Retry<ConsumerException<FalseRequeueEvent_Group>, FalseRequeueEvent_Group>(new FalseRequeueEvent_Group { Id = 1 }, (services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: false
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Exception_Requeue()
    {
        var consumer = await Retry<ConsumerException<TrueRequeueEvent_Group>, TrueRequeueEvent_Group>(new TrueRequeueEvent_Group { Id = 1 }, (services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(-1, consumer.RetryCount);
    }

    /*
         private async Task<TConsumer> Retry<TConsumer, TEvent, THandler>(TEvent @event, Action<IServiceCollection>? action = null)
        where TConsumer : class, IConsumer<TEvent>, IRetry
        where TEvent : class
        where THandler : IEventHandler<TEvent>
     */
    private async Task<TConsumer> Retry<TConsumer, TEvent>(TEvent @event, Action<IServiceCollection>? action = null)
        where TConsumer : class, IConsumer<TEvent>
        where TEvent : class
    {
        ServiceCollection services = Mock();

        var retryFactory = new TestRetryPolicyFactory(Heler.CreateLogger<DefaultRetryPolicyFactory>());
        services.AddSingleton<IRetryPolicyFactory>(retryFactory);

        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(TEvent));
        typeFilter.Filter(services, typeof(ExceptionEventHandler<TEvent>));
        typeFilter.Build(services);

        services.AddKeyedSingleton<IConsumer<TEvent>, TConsumer>("test");
        RegisterHost(services, new ConsumerType
        {
            Consumer = typeof(IConsumer<TEvent>),
            Event = typeof(TEvent),
            Queue = "test"
        });

        if (action != null)
        {
            action.Invoke(services);
        }

        var ioc = services.BuildServiceProvider();
        var jsonSerializer = ioc.GetRequiredService<IJsonSerializer>();
        var consumer = ioc.GetRequiredKeyedService<IConsumer<TEvent>>("test") as TConsumer;
        Assert.NotNull(consumer);
        using var hostService = ioc.GetRequiredService<IEnumerable<IHostedService>>().OfType<TestDefaultConsumerHostService>().First();

        await hostService.StartAsync(CancellationToken.None);
        var eventBody = new EventBody<TEvent>()
        {
            Id = 1,
            Queue = "test",
            CreationTime = DateTimeOffset.Now,
            Body = @event
        };

        var bytes = jsonSerializer.Serializer(eventBody);
        var buffer = new byte[1000];
        IAmqpWriteable _basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = "content" };
        int offset = _basicProperties.WriteTo(buffer);
        await hostService.PublishAsync<TEvent>("test", _mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        return consumer;
    }

    void RegisterHost(IServiceCollection services, params ConsumerType[] consumerTypes)
    {
        Func<IServiceProvider, TestDefaultConsumerHostService> funcFactory = (serviceProvider) =>
        {
            return new TestDefaultConsumerHostService(
                serviceProvider,
                serviceProvider.GetRequiredService<ServiceFactory>(),
                serviceProvider.GetRequiredService<ConnectionPool>(),
                serviceProvider.GetRequiredService<ILogger<ConsumerBaseHostService>>(),
                consumerTypes.ToList());
        };

        services.TryAddEnumerable(new ServiceDescriptor(serviceType: typeof(IHostedService), factory: funcFactory, lifetime: ServiceLifetime.Singleton));
    }
}
