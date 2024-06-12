using Maomi.MQ.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Sockets;
using System.Reflection;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class DefaultCustomerHostTests : BaseHostTest
{
    [Fact]
    public async Task WaitReady()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter = new ConsumerTypeFilter();
                typeFilter.Filter(services, typeof(UnSetConsumer<IdEvent>));

                services.RemoveAll<IHostedService>();
                services.AddHostedService<WaitReady_0_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();
                services.AddHostedService<WaitReady_1_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();
                services.AddHostedService<WaitReady_2_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();

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
        var s0 = ss.OfType<WaitReady_0_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>().First();
        var s1 = ss.OfType<WaitReady_1_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>().First();
        var s2 = ss.OfType<WaitReady_2_ConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>().First();

        _ = host.RunAsync();

        await Task.Delay(1000);

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
                var typeFilter = new ConsumerTypeFilter();
                typeFilter.Filter(services, typeof(UnSetConsumer<IdEvent>));

                services.RemoveAll<IHostedService>();
                services.AddHostedService<TestDefaultConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();

                // mock.
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
        await Task.Delay(1000);

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

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UnSetConsumer<IdEvent>));
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

        services.AddSingleton<AllOptionsConsumerHostService>();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(AllOptionsConsumer));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        using var host = ioc.GetRequiredService<AllOptionsConsumerHostService>();
        await host.StartAsync(CancellationToken.None);

        // check arguments.
        _mockChannel.Verify(a => a.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("1000", arguments["x-expires"]);
        Assert.Equal(string.Empty, arguments["x-dead-letter-exchange"]);
        Assert.Equal("test_dead", arguments["x-dead-letter-routing-key"]);
    }

    [Fact]
    public async Task ConsumerAsync()
    {
        ServiceCollection services = Mock();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UnSetConsumer<IdEvent>));

        // replace default consumer.
        services.Add(new ServiceDescriptor(serviceKey: "test", serviceType: typeof(IConsumer<IdEvent>), implementationType: typeof(UnSetConsumer<IdEvent>), lifetime: ServiceLifetime.Singleton));
        services.AddSingleton<TestDefaultConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();
        var ioc = services.BuildServiceProvider();

        var jsonSerializer = ioc.GetRequiredService<IJsonSerializer>();
        var consumer = ioc.GetRequiredKeyedService<IConsumer<IdEvent>>("test") as UnSetConsumer<IdEvent>;
        Assert.NotNull(consumer);
        using var hostService = ioc.GetRequiredService<TestDefaultConsumerHostService<UnSetConsumer<IdEvent>, IdEvent>>();
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
        await hostService.PublishAsync(_mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        Assert.Equal(eventBody.Id, consumer.EventBody.Id);
        Assert.Equal(eventBody.Queue, consumer.EventBody.Queue);
        Assert.Equal(eventBody.CreationTime, consumer.EventBody.CreationTime);
        Assert.Equal(eventBody.Body.Id, consumer.EventBody.Body.Id);
    }

    [Fact]
    public async Task Retry_Five_Times()
    {
        var consumer = await Retry<Exception_NoRequeue_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 });

        // Run once and retry five times
        Assert.Equal(6, consumer.RetryCount);
        Assert.True(consumer.IsFallbacked);
    }

    // Retry faild,fallback false,requeue
    [Fact]
    public async Task Retry_Faild_And_Fallback_False_Requeue()
    {
        await Retry<Retry_Faild_Fallback_False_Requeue_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Retry faild,fallback false
    [Fact]
    public async Task Retry_Faild_And_Fallback_False_NoRequeue()
    {
        await Retry<Retry_Faild_Fallback_True_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 });
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retry_Faild_And_Fallback_True()
    {
        await Retry<Retry_Faild_Fallback_True_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 });
        // requeue: false
        _mockChannel.Verify(a => a.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Exception_NoRequeue()
    {
        var consumer = await Retry<Exception_NoRequeue_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 }, (services) =>
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
            Queue = "test",
            CreationTime = DateTimeOffset.Now,
            Body = new IdEvent
            {
                Id = 1
            }
        };

        var consumer = await Retry<Exception_Requeue_Consumer<IdEvent>, IdEvent>(new IdEvent { Id = 1 }, (services) =>
        {
            services.AddSingleton<IJsonSerializer, ExceptionJsonSerializer>();
        });
        // requeue: true
        _mockChannel.Verify(a => a.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), true, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(-1, consumer.RetryCount);
    }

    private async Task<TConsumer> Retry<TConsumer, TEvent>(TEvent @event, Action<IServiceCollection>? action = null)
    where TConsumer : class, IConsumer<TEvent>, IRetry
    where TEvent : class
    {
        ServiceCollection services = Mock();

        services.AddSingleton<TestDefaultConsumerHostService<UnSetConsumer<TEvent>, TEvent>>();

        var retryFactory = new TestRetryPolicyFactory(Heler.CreateLogger<DefaultRetryPolicyFactory>());
        services.AddSingleton<IRetryPolicyFactory>(retryFactory);

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(TConsumer));
        services.Add(new ServiceDescriptor(serviceKey: "test", serviceType: typeof(IConsumer<TEvent>), implementationType: typeof(TConsumer), lifetime: ServiceLifetime.Singleton));

        if (action != null)
        {
            action.Invoke(services);
        }

        var ioc = services.BuildServiceProvider();
        var jsonSerializer = ioc.GetRequiredService<IJsonSerializer>();
        var consumer = ioc.GetRequiredKeyedService<IConsumer<TEvent>>("test") as TConsumer;
        Assert.NotNull(consumer);
        using var hostService = ioc.GetRequiredService<TestDefaultConsumerHostService<UnSetConsumer<TEvent>, TEvent>>();

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
        await hostService.PublishAsync(_mockChannel.Object, new BasicDeliverEventArgs("aa", 1, false, "test", "", new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        return consumer;
    }
}
