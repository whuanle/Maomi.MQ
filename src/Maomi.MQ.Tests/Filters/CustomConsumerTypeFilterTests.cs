using AutoFixture;
using Maomi.MQ;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class CustomConsumerTypeFilterTests
{
    [Fact]
    public void AddConsumer_ShouldAddConsumerType()
    {
        var filter = new CustomConsumerTypeFilter(); 
        Fixture fixture = new Fixture();
        int expectedNumber = fixture.Create<int>();
        var consumerOptions = fixture.Create<MockConsumerOptions>();

        filter.AddConsumer<TestConsumer>(consumerOptions);

        var services = new ServiceCollection();
        var consumerTypes = filter.Build(services);
        Assert.Single(consumerTypes);
        var consumerType = consumerTypes.First();
        Assert.Equal(consumerOptions.Queue, consumerType.Queue);
        Assert.Equal(typeof(TestConsumer), consumerType.Consumer);
        Assert.Equal(typeof(TestMessage), consumerType.Event);
        Assert.Equal(consumerOptions, consumerType.ConsumerOptions);
    }

    [Fact]
    public void AddConsumer_InvalidConsumerType_ShouldThrowException()
    {
        var filter = new CustomConsumerTypeFilter();
        var consumerOptions = new MockConsumerOptions { Queue = "test-queue" };

        Assert.Throws<NotImplementedException>(() => filter.AddConsumer(typeof(InvalidConsumer), consumerOptions));
    }

    private class MockConsumerOptions : IConsumerOptions
    {
        public string Queue { get; set; } = default!;
        public string? DeadExchange { get; set; }
        public string? DeadRoutingKey { get; set; }
        public int Expiration { get; set; }
        public ushort Qos { get; set; }
        public bool RetryFaildRequeue { get; set; }
        public AutoQueueDeclare AutoQueueDeclare { get; set; }
        public string? BindExchange { get; set; }
        public string? ExchangeType { get; set; }
        public string? RoutingKey { get; set; }

        public IConsumerOptions Clone() => (IConsumerOptions)MemberwiseClone();
        public void CopyFrom(IConsumerOptions options) => throw new NotImplementedException();
    }

    private class TestConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message)
        {
            throw new NotImplementedException();
        }

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex)
        {
            throw new NotImplementedException();
        }
    }

    private class InvalidConsumer
    {
    }

    private class TestMessage
    {
    }
}
