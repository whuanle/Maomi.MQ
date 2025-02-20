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
    public async Task ExecuteAsync(MessageHeader messageHeader,TestEvent message, EventHandlerDelegate<TestEvent> next)
    {
        await next(messageHeader, message, CancellationToken.None);
    }
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent? message) => Task.CompletedTask;
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[EventOrder(0)]
public class My1EventEventHandler : IEventHandler<TestEvent>
{
    public Task CancelAsync(TestEvent message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Message},事件 1 已被执行");
        return Task.CompletedTask;
    }
}

[EventOrder(1)]
public class My2EventEventHandler : IEventHandler<TestEvent>
{
    public Task CancelAsync(TestEvent message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Message},事件 2 已被执行");
        return Task.CompletedTask;
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
