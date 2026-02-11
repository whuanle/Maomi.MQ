using Maomi.MQ;

namespace Maomi.MQ.UnitTests.Core;

public class EmptyConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldComplete()
    {
        var consumer = new TestEmptyConsumer();
        await consumer.ExecuteAsync(new MessageHeader(), new TestMessage());
    }

    [Fact]
    public async Task FaildAsync_ShouldComplete()
    {
        var consumer = new TestEmptyConsumer();
        await consumer.FaildAsync(new MessageHeader(), new InvalidOperationException("boom"), 3, new TestMessage());
    }

    [Fact]
    public async Task FallbackAsync_ShouldReturnAck()
    {
        var consumer = new TestEmptyConsumer();
        var state = await consumer.FallbackAsync(new MessageHeader(), new TestMessage(), new Exception("e"));

        Assert.Equal(ConsumerState.Ack, state);
    }

    private sealed class TestEmptyConsumer : EmptyConsumer<TestMessage>
    {
    }

    private sealed class TestMessage
    {
    }
}
