using Maomi.MQ;
using Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Extensions;

public class PublisherExtensionsTests
{
    [Fact]
    public void CreateSingle_WithDefaultPublisher_ShouldReturnSingleChannelPublisher()
    {
        var publisher = CreateDefaultPublisher();

        var result = publisher.CreateSingle();

        Assert.IsAssignableFrom<ISingleChannelPublisher>(result);
        Assert.IsType<SingleChannelPublisher>(result);
    }

    [Fact]
    public async Task TxSelectAsync_ShouldReturnTransactionPublisher()
    {
        var publisher = CreateDefaultPublisher();

        var result = await publisher.TxSelectAsync();

        Assert.IsAssignableFrom<ITransactionPublisher>(result);
        Assert.IsType<TransactionPublisher>(result);
    }

    [Fact]
    public void CreateTransaction_ShouldReturnTransactionPublisher()
    {
        var publisher = CreateDefaultPublisher();

        var result = publisher.CreateTransaction();

        Assert.IsAssignableFrom<ITransactionPublisher>(result);
        Assert.IsType<TransactionPublisher>(result);
    }

    [Fact]
    public void CreateSingle_WithUnknownPublisher_ShouldThrow()
    {
        IMessagePublisher publisher = new UnknownPublisher();

        Assert.Throws<InvalidCastException>(() => publisher.CreateSingle());
    }

    [Fact]
    public void CreateTransaction_WithUnknownPublisher_ShouldThrow()
    {
        IMessagePublisher publisher = new UnknownPublisher();

        Assert.Throws<InvalidCastException>(() => publisher.CreateTransaction());
    }

    private static DefaultMessagePublisher CreateDefaultPublisher()
    {
        var harness = new RabbitMqTestHarness();
        harness.ServiceCollection.AddSingleton<Maomi.MQ.Diagnostics.IPublisherDiagnostics>(new Moq.Mock<Maomi.MQ.Diagnostics.IPublisherDiagnostics>().Object);

        var provider = harness.BuildProvider();
        return new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());
    }

    private sealed class UnknownPublisher : IMessagePublisher
    {
        public Task AutoPublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null, CancellationToken cancellationToken = default)
            where TMessage : class
            => Task.CompletedTask;

        public Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, Action<BasicProperties> properties, CancellationToken cancellationToken = default)
            where TMessage : class
            => Task.CompletedTask;

        public Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
