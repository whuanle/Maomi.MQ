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
    public async Task ExecuteAsync(EventBody<TestEvent> message, EventHandlerDelegate<TestEvent> next)
    {
        await next(message, CancellationToken.None);
    }
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[EventOrder(0)]
public class My1EventEventHandler : IEventHandler<TestEvent>
{
    public async Task CancelAsync(EventBody<TestEvent> message, CancellationToken cancellationToken)
    {
    }

    public async Task ExecuteAsync(EventBody<TestEvent> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Id},事件 1 已被执行");
    }
}

[EventOrder(1)]
public class My2EventEventHandler : IEventHandler<TestEvent>
{
    public async Task CancelAsync(EventBody<TestEvent> message, CancellationToken cancellationToken)
    {
    }

    public async Task ExecuteAsync(EventBody<TestEvent> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Id},事件 2 已被执行");
    }
}


[EventTopic("EventWeb_dynamic")]
public class DynamicTestEvent
{
    public string Message { get; set; }

    public override string ToString()
    {
        return Message;
    }
}

[EventOrder(0)]
public class DynamicEventEventHandler : IEventHandler<DynamicTestEvent>
{
    public async Task CancelAsync(EventBody<DynamicTestEvent> message, CancellationToken cancellationToken)
    {
    }

    public async Task ExecuteAsync(EventBody<DynamicTestEvent> message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Id},事件 1 已被执行");
    }
}
