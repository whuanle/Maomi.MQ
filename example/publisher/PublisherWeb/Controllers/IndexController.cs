using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace PublisherWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        for (var i = 0; i < 100; i++)
        {
            await _messagePublisher.PublishAsync(queue: "PublisherWeb", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }
}

[Consumer("PublisherWeb", Qos = 1, RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
