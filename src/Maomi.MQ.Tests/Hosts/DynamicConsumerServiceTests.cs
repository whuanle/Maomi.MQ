using AutoFixture;
using AutoFixture.Xunit2;
using Maomi.MQ;
using Maomi.MQ.Default;
using Maomi.MQ.EventBus;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Maomi.MQ.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using RabbitMQ.Client;
using System.Collections.Concurrent;

public class DynamicConsumerServiceTests
{
    [Fact]
    public async Task ConsumerAsync_ShouldThrowException_WhenQueueAlreadyUsed()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var autofix = new AutoFixture.Fixture();
        var consumerOptions = autofix.Create<ConsumerOptions>();
        var consumerType = new ConsumerType
        {
            Consumer = typeof(EmptyConsumer<TestMessage>),
            ConsumerOptions = consumerOptions,
            Event = typeof(TestMessage),
            Queue = consumerOptions.Queue
        };

        var serviceProvider = services.BuildServiceProvider();
        MockDynamicConsumerService dynamicConsumerService = new MockDynamicConsumerService(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            rabbitMQConnectionMock.ConnectionPoolMock.Object,
            new ConsumerTypeProvider() { consumerType });

        await Assert.ThrowsAsync<ArgumentException>(() => dynamicConsumerService.ConsumerAsync<TestMessage>(consumerOptions.Clone()));
    }

    [Theory, AutoData]
    public async Task ConsumerAsync_ShouldAddConsumer_WhenQueueNotUsed(string mockConsumerTag)
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();
        var dynamicConsumerServiceMock = new Mock<DynamicConsumerService>(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            rabbitMQConnectionMock.ConnectionPoolMock.Object,
            new ConsumerTypeProvider { }
            )
        { CallBase = true };

        dynamicConsumerServiceMock.Protected().Setup<Task<string>>(
            methodOrPropertyName: "CreateMessageConsumer",
            genericTypeArguments: new Type[] { typeof(TestMessage) },
        exactParameterMatch: false,
            args: new object[] { ItExpr.IsAny<IChannel>(), ItExpr.IsAny<Type>(), ItExpr.IsAny<IConsumerOptions>() }).Returns(Task.FromResult(mockConsumerTag));

        var consumerOptions = Mock.Of<IConsumerOptions>(x => x.Queue == "test-queue");

        var consumerTag = await dynamicConsumerServiceMock.Object.ConsumerAsync<TestMessage>(consumerOptions);

        Assert.NotNull(consumerTag);
        Assert.Equal(mockConsumerTag, consumerTag);
    }

    [Fact]
    public async Task StopConsumerAsync_ShouldRemoveConsumer_WhenQueueExists()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);
        var serviceProvider = services.BuildServiceProvider();

        var queue = "test-queue";
        var consumerTag = "test-consumer-tag";
        var dynamicConsumerService = new Mock<MockDynamicConsumerService>(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            serviceProvider.GetRequiredService<ConnectionPool>(),
            new ConsumerTypeProvider()
            )
        { CallBase = true };

        dynamicConsumerService.Object.TryAdd(queue, consumerTag);

        await dynamicConsumerService.Object.StopConsumerAsync(queue);

        Assert.False(dynamicConsumerService.Object.Consumers.ContainsKey(queue));
    }

    [Fact]
    public async Task StopConsumerTagAsync_ShouldRemoveConsumer_WhenConsumerTagExists()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);
        var serviceProvider = services.BuildServiceProvider();

        var queue = "test-queue";
        var consumerTag = "test-consumer-tag";
        var dynamicConsumerService = new Mock<MockDynamicConsumerService>(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            serviceProvider.GetRequiredService<ConnectionPool>(),
            new ConsumerTypeProvider()
            )
        { CallBase = true };

        dynamicConsumerService.Object.Consumers.TryAdd(queue, consumerTag);

        await dynamicConsumerService.Object.StopConsumerTagAsync(consumerTag);

        Assert.False(dynamicConsumerService.Object.Consumers.ContainsKey(queue));
    }

    [Theory, AutoData]
    public async Task ConsumerAsync_ShouldAddEventBusConsumer(ConsumerOptions consumerOptions, string mockConsumerTag)
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);
        var serviceProvider = services.BuildServiceProvider();

        var dynamicConsumerServiceMock = new Mock<DynamicConsumerService>(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            serviceProvider.GetRequiredService<ConnectionPool>(),
            new ConsumerTypeProvider()
            )
        {
            CallBase = true
        };

        // ConsumerAsync<TMessage>(IConsumerOptions consumerOptions)
        // => 
        // ConsumerAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions)

        dynamicConsumerServiceMock.Setup(x => x.ConsumerAsync<EventBusConsumer<TestMessage>, TestMessage>(It.IsAny<IConsumerOptions>())).ReturnsAsync(mockConsumerTag);

        var consumerTag = await dynamicConsumerServiceMock.Object.ConsumerAsync<TestMessage>(consumerOptions);

        Assert.Equal(mockConsumerTag, consumerTag);
        dynamicConsumerServiceMock.Verify(x => x.ConsumerAsync<EventBusConsumer<TestMessage>, TestMessage>(consumerOptions), Times.Once);
    }

    [Theory, AutoData]
    public async Task ConsumerAsync_ShouldAddDynamicProxyConsumer(ConsumerOptions consumerOptions, string mockConsumerTag)
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);
        var serviceProvider = services.BuildServiceProvider();

        var dynamicConsumerService = new Mock<MockDynamicConsumerService>(
            serviceProvider.GetRequiredService<ServiceFactory>(),
            serviceProvider.GetRequiredService<ConnectionPool>(),
            new ConsumerTypeProvider()
            )
        {
            CallBase = true
        };

        dynamicConsumerService.Protected().Setup<Task<string>>("CreateDynamicMessageConsumer",
           genericTypeArguments: new Type[] { typeof(TestMessage) },
           exactParameterMatch: false,
           args: new object[] { ItExpr.IsAny<IChannel>(), ItExpr.IsAny<IConsumerOptions>(), ItExpr.IsAny<DynamicProxyConsumer<TestMessage>>() }).ReturnsAsync(mockConsumerTag);

        var consumerTagResult = await dynamicConsumerService.Object.ConsumerAsync<TestMessage>(
            consumerOptions,
            execute: async (h, m) => { await Task.CompletedTask; });

        Assert.Equal(mockConsumerTag, consumerTagResult);
    }

    private static ServiceCollection BuildServiceCollection(RabbitMQConnectionMock rabbitMQConnectionMock)
    {
        var services = new ServiceCollection();
        services.AddMaomiMQCore();
        services.AddLogging();
        services.AddSingleton(rabbitMQConnectionMock.MqOptions);
        services.AddScoped<ServiceFactory>();
        services.AddScoped<IBreakdown, DefaultBreakdown>();
        services.AddSingleton<ConnectionPool>(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddSingleton<IRoutingProvider, RoutingProvider>();
        return services;
    }

    public class TestMessage { }

    public class MockDynamicConsumerService : DynamicConsumerService
    {
        public MockDynamicConsumerService(ServiceFactory serviceFactory, ConnectionPool connectionPool, IConsumerTypeProvider consumerTypeProvider)
            : base(serviceFactory, connectionPool, consumerTypeProvider)
        {
        }

        public void TryAdd(string queue, string consumerTag)
        {
            _consumers.TryAdd(queue, consumerTag);
        }

        public void TryRemove(string queue)
        {
            _consumers.TryRemove(queue, out _);
        }

        public ConcurrentDictionary<string, string> Consumers => _consumers;
    }
}
