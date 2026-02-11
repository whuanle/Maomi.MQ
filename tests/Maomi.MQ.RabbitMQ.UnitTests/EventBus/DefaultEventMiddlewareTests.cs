using Maomi.MQ;
using Maomi.MQ.EventBus;

namespace Maomi.MQ.RabbitMQ.UnitTests.EventBus;

public class DefaultEventMiddlewareTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeNextDelegate()
    {
        var middleware = new DefaultEventMiddleware<TestMessage>();
        var invoked = false;

        await middleware.ExecuteAsync(
            new MessageHeader(),
            new TestMessage(),
            (header, message, token) =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

        Assert.True(invoked);
    }

    [Fact]
    public async Task FaildAsync_ShouldComplete()
    {
        var middleware = new DefaultEventMiddleware<TestMessage>();
        await middleware.FaildAsync(new MessageHeader(), new Exception("boom"), 1, new TestMessage());
    }

    [Fact]
    public async Task FallbackAsync_ShouldReturnAck()
    {
        var middleware = new DefaultEventMiddleware<TestMessage>();
        var state = await middleware.FallbackAsync(new MessageHeader(), new TestMessage(), new Exception("boom"));

        Assert.Equal(ConsumerState.Ack, state);
    }

    private sealed class TestMessage
    {
    }
}
