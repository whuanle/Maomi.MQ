using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class DefaultCustomerHostTests : BaseHostTests
{
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
    public async Task ConsumerAsync()
    {
        ServiceCollection services = Mock();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UnSetConsumer<IdEvent>));

        // replace default consumer lifetime.
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

        _ = hostService.StartAsync(CancellationToken.None);

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

        var retryFactory = new TestRetryPolicyFactory(Heler.CreateLogger<DefaultRetryPolicyFactory>());
        services.AddSingleton<IRetryPolicyFactory>(retryFactory);

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(TConsumer));
        services.Add(new ServiceDescriptor(serviceKey: "test", serviceType: typeof(IConsumer<TEvent>), implementationType: typeof(TConsumer), lifetime: ServiceLifetime.Singleton));

        RegisterHost(services, new ConsumerType
        {
            Consumer = typeof(TConsumer),
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

        _ = hostService.StartAsync(CancellationToken.None);

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
}
