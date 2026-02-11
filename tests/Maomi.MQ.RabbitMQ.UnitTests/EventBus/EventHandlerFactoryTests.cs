using Maomi.MQ.EventBus;

namespace Maomi.MQ.RabbitMQ.UnitTests.EventBus;

public class EventHandlerFactoryTests
{
    [Fact]
    public void Constructor_ShouldStoreHandlers()
    {
        var handlers = new Dictionary<int, Type>
        {
            [1] = typeof(string),
            [2] = typeof(int),
        };

        var factory = new EventHandlerFactory<TestMessage>(handlers);

        Assert.Same(handlers, factory.Handlers);
        Assert.Equal(2, factory.Handlers.Count);
    }

    private sealed class TestMessage
    {
    }
}
