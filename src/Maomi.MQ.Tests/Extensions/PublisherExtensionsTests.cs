using Maomi.MQ;
using Maomi.MQ.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;

public class PublisherExtensionsTests
{
    [Fact]
    public void CreateSingle_ShouldReturnSingleChannelPublisher()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMaomiMQCore();
        services.AddSingleton(rabbitMQConnectionMock.MqOptions);
        services.AddSingleton(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddScoped<IMessagePublisher, DefaultMessagePublisher>();
        var serviceProvider = services.BuildServiceProvider();

        var mockMessagePublisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        var createChannelOptions = new RabbitMQ.Client.CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false);

        var result = PublisherExtensions.CreateSingle(mockMessagePublisher, createChannelOptions);

        Assert.NotNull(result);
        Assert.IsType<SingleChannelPublisher>(result);
    }

    [Fact]
    public void CreateSingle_ShouldThrowInvalidCastException()
    {
        var mockMessagePublisher = new Mock<IMessagePublisher>();

        Assert.Throws<InvalidCastException>(() => PublisherExtensions.CreateSingle(mockMessagePublisher.Object));
    }

    [Fact]
    public async Task TxSelectAsync_ShouldReturnTransactionPublisher()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMaomiMQCore();
        services.AddSingleton(rabbitMQConnectionMock.MqOptions);
        services.AddSingleton(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddScoped<IMessagePublisher, DefaultMessagePublisher>();
        var serviceProvider = services.BuildServiceProvider();

        var mockMessagePublisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        var result = await PublisherExtensions.TxSelectAsync(mockMessagePublisher);

        Assert.NotNull(result);
        Assert.IsType<TransactionPublisher>(result);
    }

    [Fact]
    public void CreateTransaction_ShouldReturnTransactionPublisher()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMaomiMQCore();
        services.AddSingleton(rabbitMQConnectionMock.MqOptions);
        services.AddSingleton(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddScoped<IMessagePublisher, DefaultMessagePublisher>();
        var serviceProvider = services.BuildServiceProvider();

        var mockMessagePublisher = serviceProvider.GetRequiredService<IMessagePublisher>();

        var result = PublisherExtensions.CreateTransaction(mockMessagePublisher);

        Assert.NotNull(result);
        Assert.IsType<TransactionPublisher>(result);
    }

    [Fact]
    public void CreateTransaction_ShouldThrowInvalidCastException()
    {
        var mockMessagePublisher = new Mock<IMessagePublisher>();

        Assert.Throws<InvalidCastException>(() => PublisherExtensions.CreateTransaction(mockMessagePublisher.Object));
    }
}
