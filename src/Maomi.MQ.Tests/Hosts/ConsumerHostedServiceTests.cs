using AutoFixture;
using Maomi.MQ.Default;
using Maomi.MQ.Filters;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using RabbitMQ.Client;

namespace Maomi.MQ.Tests;

public class ConsumerHostedServiceTests
{
    [Fact]
    public async Task WaitReadyInitQueueAsync_ShouldCallInitQueueAsyncOnce()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();

        Mock<IRoutingProvider> _routingProviderMock = new Mock<IRoutingProvider>();
        _routingProviderMock.Setup(x => x.Get(It.IsAny<IConsumerOptions>())).Returns(new ConsumerOptions("test-queue_2")
        {
            DeadExchange = "test-dead-exchange_2",
            DeadRoutingKey = "test-dead-routing-key_2",
            Expiration = 60000,
            Qos = 10,
            RetryFaildRequeue = true,
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            BindExchange = "test-bind-exchange_2",
            ExchangeType = "direct",
            RoutingKey = "test-routing_2"
        });

        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var consumerTypeFilter = new ConsumerTypeFilter();
        var consumerType = typeof(TestConsumer);
        consumerTypeFilter.Filter(services, consumerType);
        var consumerTypes = consumerTypeFilter.Build(services).ToList();

        services.AddSingleton<ConnectionPool>(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddSingleton(_routingProviderMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var consumerHostedServiceMock = new Mock<ConsumerHostedServiceMock>(serviceProvider.GetRequiredService<ServiceFactory>(), serviceProvider.GetRequiredService<ConnectionPool>(), consumerTypes) { CallBase = true};
        consumerHostedServiceMock.Protected().Setup<Task<string>>(
            methodOrPropertyName: "CreateMessageConsumer",
            genericTypeArguments: new Type[] {  typeof(TestEvent) },
            exactParameterMatch: false,
            args: new object[] { ItExpr.IsAny<IChannel>(), ItExpr.IsAny<Type>(), ItExpr.IsAny<Type>(), ItExpr.IsAny<IConsumerOptions>() }).Returns(Task.FromResult("consumer-tag"));

        await consumerHostedServiceMock.Object.MockWaitReadyInitQueueAsync();

        _routingProviderMock.Verify(x => x.Get(It.Is<IConsumerOptions>(co => co.Queue == "test-queue")), Times.Once);
        consumerHostedServiceMock.Protected().Verify<Task>("InitQueueAsync", Times.Once(), new object[] { ItExpr.IsAny<IChannel>(), ItExpr.Is<IConsumerOptions>(x => x.Queue == "test-queue_2") });
    }

    [Fact]
    public async Task ShouldInitializeNormally_CompleteProcess()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        var consumerOptions = new ConsumerOptions("test-queue_2")
        {
            DeadExchange = "test-dead-exchange_2",
            DeadRoutingKey = "test-dead-routing-key_2",
            Expiration = 60000,
            Qos = 10,
            RetryFaildRequeue = true,
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            BindExchange = "test-bind-exchange_2",
            ExchangeType = "direct",
            RoutingKey = "test-routing_2"
        };

        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var consumerTypeFilter = new CustomConsumerTypeFilter();
        var consumerType = typeof(TestConsumer);
        consumerTypeFilter.AddConsumer(consumerType, consumerOptions);
        var consumerTypes = consumerTypeFilter.Build(services).ToList();

        var serviceProvider = services.BuildServiceProvider();
        var consumerHostedServiceMock = new Mock<ConsumerHostedServiceMock>(serviceProvider.GetRequiredService<ServiceFactory>(), serviceProvider.GetRequiredService<ConnectionPool>(), consumerTypes) { CallBase = true };
        consumerHostedServiceMock.Protected().Setup<Task<string>>(
            methodOrPropertyName: "CreateMessageConsumer",
            genericTypeArguments: new Type[] { typeof(TestEvent) },
            exactParameterMatch: false,
            args: new object[] { ItExpr.IsAny<IChannel>(), ItExpr.IsAny<Type>(), ItExpr.IsAny<Type>(), ItExpr.IsAny<IConsumerOptions>() }).Returns(Task.FromResult("consumer-tag"));


        await consumerHostedServiceMock.Object.MockWaitReadyInitQueueAsync();

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.QueueDeclareAsync(
            consumerOptions.Queue,
            true,
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.Is<IDictionary<string, object?>>(a => a["x-expires"]!.ToString() == consumerOptions.Expiration.ToString() && a["x-dead-letter-exchange"]!.ToString() == consumerOptions.DeadExchange && a["x-dead-letter-routing-key"]!.ToString() == consumerOptions.DeadRoutingKey),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // IChannelExtensions.ExchangeDeclareAsync => channel.ExchangeDeclareAsync
        rabbitMQConnectionMock.ChannelMock.Verify(x => x.ExchangeDeclareAsync(
            consumerOptions.BindExchange,
            consumerOptions.ExchangeType,
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.QueueBindAsync(
            consumerOptions.Queue,
            consumerOptions.BindExchange,
            consumerOptions.RoutingKey,
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
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

    [Consumer("test-queue",
        AutoQueueDeclare =  AutoQueueDeclare.Enable,
        Expiration = 10000,
        DeadRoutingKey = "test-routingkey",
        DeadExchange = "test-exchange")]
    private class TestConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
        {
            throw new NotImplementedException();
        }
    }

    public class TestEvent { }

    public class ConsumerHostedServiceMock : ConsumerHostedService
    {
        public ConsumerHostedServiceMock(ServiceFactory serviceFactory, ConnectionPool connectionPool, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceFactory, connectionPool, consumerTypes)
        {
        }

        public Task MockWaitReadyInitQueueAsync()
        {
            return base.WaitReadyInitQueueAsync();
        }
    }
}
