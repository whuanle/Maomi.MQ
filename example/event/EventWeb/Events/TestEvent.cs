using Maomi.MQ;
using Maomi.MQ.EventBus;

namespace EventWeb.Events;

[EventTopic("EventWeb")]
public class TestEvent
{
    public string Message { get; set; }

    public override string ToString()
    {
        return Message;
    }
}


public class TestEventMiddleware : IEventMiddleware<TestEvent>
{
    public async Task ExecuteAsync(EventBody<TestEvent> @event, EventHandlerDelegate<TestEvent> next)
    {
        await next(@event, CancellationToken.None);
    }
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[EventOrder(0)]
public class My1EventEventHandler : IEventHandler<TestEvent>
{
    public async Task CancelAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
    {
    }

    public async Task ExecuteAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{@event.Id},事件 1 已被执行");
    }
}

[EventOrder(1)]
public class My2EventEventHandler : IEventHandler<TestEvent>
{
    public async Task CancelAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
    {
    }

    public async Task ExecuteAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{@event.Id},事件 2 已被执行");
    }
}
