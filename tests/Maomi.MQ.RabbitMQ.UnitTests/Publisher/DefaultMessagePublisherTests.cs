using Maomi.MQ;
using Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Publisher;

public class DefaultMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_ShouldPublishUsingDefaultChannel()
    {
        var harness = new RabbitMqTestHarness();
        var provider = harness.BuildProvider();

        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await publisher.PublishAsync("ex", "route", new MessageA { Value = 1 });

        harness.SharedChannelMock.Verify(x => x.BasicPublishAsync(
            "ex",
            "route",
            true,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoPublishAsync_WithQueueNameAttribute_ShouldResolveRouteAndPublish()
    {
        var harness = new RabbitMqTestHarness();
        var provider = harness.BuildProvider();

        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await publisher.AutoPublishAsync(new MessageWithQueueName { Value = 2 });

        harness.SharedChannelMock.Verify(x => x.BasicPublishAsync(
            "exchange-a",
            "route-a",
            true,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoPublishAsync_WithoutQueueNameAttribute_ShouldThrow()
    {
        var harness = new RabbitMqTestHarness();
        var provider = harness.BuildProvider();

        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.AutoPublishAsync(new MessageWithoutQueueName()));
    }

    [Fact]
    public async Task PublishAsync_WhenNoSuitableSerializer_ShouldThrow()
    {
        var harness = new RabbitMqTestHarness();

        var options = new MqOptions
        {
            AppName = harness.MqOptions.AppName,
            WorkId = harness.MqOptions.WorkId,
            AutoQueueDeclare = harness.MqOptions.AutoQueueDeclare,
            ConnectionFactory = harness.MqOptions.ConnectionFactory,
            MessageSerializers =
            [
                new FakeMessageSerializer("application/json", verify: _ => false)
            ]
        };

        harness.ServiceCollection.AddSingleton(options);
        var provider = harness.BuildProvider();

        var publisher = new DefaultMessagePublisher(
            provider,
            options,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync("ex", "route", new MessageA()));
    }

    [Fact]
    public async Task PublishChannelAsync_WithRawPayload_ShouldPublish()
    {
        var harness = new RabbitMqTestHarness();
        var provider = harness.BuildProvider();

        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        var header = new MessageHeader
        {
            Id = "id-1",
            AppId = "app",
            ContentType = "application/json",
            Type = "message",
            Timestamp = DateTimeOffset.UtcNow,
            Exchange = "ex",
            RoutingKey = "route",
        };

        await publisher.PublishChannelAsync(
            harness.SharedChannelMock.Object,
            "ex",
            "route",
            header,
            [1, 2, 3],
            new BasicProperties());

        harness.SharedChannelMock.Verify(x => x.BasicPublishAsync(
            "ex",
            "route",
            true,
            It.IsAny<BasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(b => b.Length == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WhenCanceled_ShouldThrowOperationCanceledException()
    {
        var harness = new RabbitMqTestHarness();
        harness.SharedChannelMock
            .Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var provider = harness.BuildProvider();
        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await Assert.ThrowsAsync<OperationCanceledException>(() => publisher.PublishAsync("ex", "route", new MessageA()));
    }

    [Fact]
    public async Task PublishAsync_WhenChannelThrows_ShouldWrapThroughDiagnosticsAndRethrow()
    {
        var harness = new RabbitMqTestHarness();
        harness.SharedChannelMock
            .Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("publish failed"));

        var provider = harness.BuildProvider();
        var publisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync("ex", "route", new MessageA()));
    }

    [RouterKey("exchange-a", "route-a")]
    private sealed class MessageWithQueueName
    {
        public int Value { get; set; }
    }

    private sealed class MessageWithoutQueueName
    {
    }

    private sealed class MessageA
    {
        public int Value { get; set; }
    }
}
