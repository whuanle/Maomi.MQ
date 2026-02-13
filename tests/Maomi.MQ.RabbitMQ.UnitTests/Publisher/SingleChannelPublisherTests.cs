using Maomi.MQ;
using Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Publisher;

public class SingleChannelPublisherTests
{
    [Fact]
    public async Task CustomPublishAsync_ShouldUseDedicatedChannel()
    {
        var harness = new RabbitMqTestHarness();
        var dedicated = harness.CreateAdditionalChannel("single-ctag");

        var provider = harness.BuildProvider();
        var basePublisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        using var single = basePublisher.CreateSingle();

        await single.CustomPublishAsync("ex", "route", new DemoMessage());

        dedicated.Verify(x => x.BasicPublishAsync(
            "ex",
            "route",
            true,
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CustomPublishAsync_WithNullProperties_ShouldCreatePersistentDefaultProperties()
    {
        var harness = new RabbitMqTestHarness();
        var dedicated = harness.CreateAdditionalChannel("single-ctag");

        BasicProperties? captured = null;
        dedicated
            .Setup(x => x.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>((_, _, _, p, _, _) => captured = p)
            .Returns(ValueTask.CompletedTask);

        var provider = harness.BuildProvider();
        var basePublisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        using var single = basePublisher.CreateSingle();
        await single.CustomPublishAsync("ex", "route", new DemoMessage(), properties: null);

        Assert.NotNull(captured);
        Assert.Equal(DeliveryModes.Persistent, captured!.DeliveryMode);
    }

    [Fact]
    public void Dispose_ShouldDisposeDedicatedChannel()
    {
        var harness = new RabbitMqTestHarness();
        var dedicated = harness.CreateAdditionalChannel("single-ctag");

        var provider = harness.BuildProvider();
        var basePublisher = new DefaultMessagePublisher(
            provider,
            harness.MqOptions,
            harness.ConnectionPool,
            provider.GetRequiredService<IIdProvider>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>());

        var single = basePublisher.CreateSingle();
        single.Dispose();

        dedicated.Verify(x => x.Dispose(), Times.Once);
    }

    private sealed class DemoMessage
    {
    }
}
