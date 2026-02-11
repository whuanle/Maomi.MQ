using Maomi.MQ;
using Maomi.MQ.Hosts;

namespace Maomi.MQ.RabbitMQ.UnitTests.Hosts;

public class DynamicProxyConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeExecuteDelegate()
    {
        var called = false;
        var consumer = new DynamicProxyConsumer<TestMessage>(
            execute: (header, message) =>
            {
                called = true;
                return Task.CompletedTask;
            },
            faild: null,
            fallback: null);

        await consumer.ExecuteAsync(new MessageHeader(), new TestMessage());

        Assert.True(called);
    }

    [Fact]
    public async Task FaildAsync_WhenNoDelegate_ShouldComplete()
    {
        var consumer = new DynamicProxyConsumer<TestMessage>(
            execute: (header, message) => Task.CompletedTask,
            faild: null,
            fallback: null);

        await consumer.FaildAsync(new MessageHeader(), new Exception("boom"), 1, new TestMessage());
    }

    [Fact]
    public async Task FaildAsync_WhenDelegateExists_ShouldInvokeDelegate()
    {
        var called = false;
        var consumer = new DynamicProxyConsumer<TestMessage>(
            execute: (header, message) => Task.CompletedTask,
            faild: (header, exception, retryCount, message) =>
            {
                called = true;
                return Task.CompletedTask;
            },
            fallback: null);

        await consumer.FaildAsync(new MessageHeader(), new Exception("boom"), 1, new TestMessage());

        Assert.True(called);
    }

    [Fact]
    public async Task FallbackAsync_WhenNoDelegate_ShouldReturnAck()
    {
        var consumer = new DynamicProxyConsumer<TestMessage>(
            execute: (header, message) => Task.CompletedTask,
            faild: null,
            fallback: null);

        var state = await consumer.FallbackAsync(new MessageHeader(), new TestMessage(), new Exception("boom"));

        Assert.Equal(ConsumerState.Ack, state);
    }

    [Fact]
    public async Task FallbackAsync_WhenDelegateExists_ShouldInvokeDelegate()
    {
        var consumer = new DynamicProxyConsumer<TestMessage>(
            execute: (header, message) => Task.CompletedTask,
            faild: null,
            fallback: (header, message, exception) => Task.FromResult(ConsumerState.NackAndRequeue));

        var state = await consumer.FallbackAsync(new MessageHeader(), new TestMessage(), new Exception("boom"));

        Assert.Equal(ConsumerState.NackAndRequeue, state);
    }

    private sealed class TestMessage
    {
    }
}
