using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.UnitTests.Filters;

public class ConsumerTypeFilterTests
{
    [Fact]
    public void Filter_WithNonConsumerType_ShouldIgnore()
    {
        var filter = new ConsumerTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(string));

        Assert.Empty(filter.Build(services));
        Assert.Empty(services);
    }

    [Fact]
    public void Filter_WithConsumerAndAttribute_ShouldRegister()
    {
        var filter = new ConsumerTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestConsumer));
        var consumers = filter.Build(services).ToArray();

        Assert.Single(consumers);
        Assert.Equal("queue-a", consumers[0].Queue);
        Assert.Equal(typeof(TestConsumer), consumers[0].Consumer);
        Assert.Equal(typeof(TestMessage), consumers[0].Event);
        Assert.Contains(services, x => x.ServiceType == typeof(TestConsumer));
    }

    [Fact]
    public void Filter_WithDuplicateQueue_ShouldThrow()
    {
        var filter = new ConsumerTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestConsumer));

        Assert.Throws<ArgumentException>(() => filter.Filter(services, typeof(TestConsumerDuplicate)));
    }

    [Fact]
    public void Filter_WithInterceptorReject_ShouldNotRegister()
    {
        var filter = new ConsumerTypeFilter((options, type) => (false, options));
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestConsumer));

        Assert.Empty(filter.Build(services));
    }

    [Fact]
    public void Filter_WithInterceptorMutate_ShouldUseMutatedOptions()
    {
        var filter = new ConsumerTypeFilter((options, type) =>
        {
            var clone = options.Clone();
            clone.CopyFrom(new ConsumerAttribute("queue-b")
            {
                BindExchange = "ex",
                ExchangeType = ExchangeType.Topic,
                RoutingKey = "route-b",
            });

            return (true, clone);
        });

        var services = new ServiceCollection();
        filter.Filter(services, typeof(TestConsumer));

        var consumer = Assert.Single(filter.Build(services));
        Assert.Equal("queue-b", consumer.Queue);
        Assert.Equal("ex", consumer.ConsumerOptions.BindExchange);
        Assert.Equal(ExchangeType.Topic, consumer.ConsumerOptions.ExchangeType);
        Assert.Equal("route-b", consumer.ConsumerOptions.RoutingKey);
    }

    [Consumer("queue-a")]
    private sealed class TestConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
    }

    [Consumer("queue-a")]
    private sealed class TestConsumerDuplicate : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
    }

    private sealed class TestMessage
    {
    }
}
