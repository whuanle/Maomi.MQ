using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.UnitTests.Filters;

public class EventBusTypeFilterTests
{
    [Fact]
    public void Filter_WithNoMiddleware_ShouldThrowOnBuildDueToIncompleteEventInfo()
    {
        var filter = new EventBusTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestHandler1));

        Assert.Throws<NullReferenceException>(() => filter.Build(services).ToArray());
    }

    [Fact]
    public void Filter_WithValidMiddlewareAndHandlers_ShouldRegisterAndBuildConsumer()
    {
        var filter = new EventBusTypeFilter();
        var services = new ServiceCollection();
        services.AddLogging();

        filter.Filter(services, typeof(TestMiddleware));
        filter.Filter(services, typeof(TestHandler1));
        filter.Filter(services, typeof(TestHandler2));

        var consumers = filter.Build(services).ToArray();

        Assert.Single(consumers);
        Assert.Equal("event-queue", consumers[0].Queue);
        Assert.Equal(typeof(TestEvent), consumers[0].Event);
        Assert.Equal(typeof(EventBusConsumer<TestEvent>), consumers[0].Consumer);

        using var provider = services.BuildServiceProvider();
        Assert.IsType<TestMiddleware>(provider.GetRequiredService<IEventMiddleware<TestEvent>>());
        Assert.IsType<HandlerMediator<TestEvent>>(provider.GetRequiredService<IHandlerMediator<TestEvent>>());
        Assert.IsType<EventBusConsumer<TestEvent>>(provider.GetRequiredService<IConsumer<TestEvent>>());

        var factory = provider.GetRequiredService<IEventHandlerFactory<TestEvent>>();
        Assert.Equal(2, factory.Handlers.Count);
        Assert.Equal(typeof(TestHandler1), factory.Handlers[1]);
        Assert.Equal(typeof(TestHandler2), factory.Handlers[2]);
    }

    [Fact]
    public void Filter_HandlerWithoutOrder_ShouldThrow()
    {
        var filter = new EventBusTypeFilter();
        var services = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => filter.Filter(services, typeof(NoOrderHandler)));
        Assert.Contains("NoOrderHandler", ex.Message);
    }

    [Fact]
    public void Filter_DuplicateHandlerOrder_ShouldThrow()
    {
        var filter = new EventBusTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestHandler1));
        var ex = Assert.Throws<ArgumentException>(() => filter.Filter(services, typeof(TestHandlerDuplicateOrder)));

        Assert.Contains("Order", ex.Message);
    }

    [Fact]
    public void Filter_WithInterceptorRejectMiddleware_ShouldSkipBuildConsumer()
    {
        var filter = new EventBusTypeFilter((options, type) => (false, options));
        var services = new ServiceCollection();

        filter.Filter(services, typeof(TestMiddleware));
        filter.Filter(services, typeof(TestHandler1));

        Assert.Throws<NullReferenceException>(() => filter.Build(services).ToArray());
    }

    [Fact]
    public void Filter_DefaultEventMiddlewareGenericType_ShouldIgnore()
    {
        var filter = new EventBusTypeFilter();
        var services = new ServiceCollection();

        filter.Filter(services, typeof(DefaultEventMiddleware<>));

        Assert.Empty(services);
    }

    [Consumer("event-queue")]
    private sealed class TestMiddleware : IEventMiddleware<TestEvent>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message, EventHandlerDelegate<TestEvent> next)
            => next(messageHeader, message, CancellationToken.None);

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent? message)
            => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
            => Task.FromResult(ConsumerState.Ack);
    }

    [EventOrder(1)]
    private sealed class TestHandler1 : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CancelAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventOrder(2)]
    private sealed class TestHandler2 : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CancelAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventOrder(1)]
    private sealed class TestHandlerDuplicateOrder : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CancelAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoOrderHandler : IEventHandler<TestEvent>
    {
        public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CancelAsync(TestEvent message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestEvent
    {
    }
}
