using Maomi.MQ;
using Maomi.MQ.Consumer;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.UnitTests.Filters;

public class CustomConsumerTypeFilterTests
{
    [Fact]
    public void AddConsumer_Generic_ShouldRegisterInBuildResult()
    {
        var filter = new CustomConsumerTypeFilter();
        var options = new ConsumerOptions { Queue = "queue-a" };
        var services = new ServiceCollection();

        filter.AddConsumer<TestConsumer>(options);
        var consumers = filter.Build(services).ToArray();

        Assert.Single(consumers);
        Assert.Equal("queue-a", consumers[0].Queue);
        Assert.Equal(typeof(TestConsumer), consumers[0].Consumer);
        Assert.Equal(typeof(TestMessage), consumers[0].Event);
        Assert.Same(options, consumers[0].ConsumerOptions);
        Assert.Contains(services, x => x.ServiceType == typeof(TestConsumer));
    }

    [Fact]
    public void AddConsumer_WithType_ShouldRegisterInBuildResult()
    {
        var filter = new CustomConsumerTypeFilter();
        var options = new ConsumerOptions { Queue = "queue-a" };
        var services = new ServiceCollection();

        filter.AddConsumer(typeof(TestConsumer), options);
        var consumers = filter.Build(services).ToArray();

        Assert.Single(consumers);
        Assert.Equal(typeof(TestConsumer), consumers[0].Consumer);
    }

    [Fact]
    public void AddConsumer_WithInvalidType_ShouldThrow()
    {
        var filter = new CustomConsumerTypeFilter();

        Assert.Throws<NotImplementedException>(() => filter.AddConsumer(typeof(string), new ConsumerOptions { Queue = "q" }));
    }

    [Fact]
    public void Filter_ShouldBeNoOp()
    {
        var filter = new CustomConsumerTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestConsumer));

        Assert.Empty(services);
    }

    private sealed class TestConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
    }

    private sealed class TestMessage
    {
    }
}
