using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;

public class MyConsumer : IConsumer<TestEvent>
{
    private readonly ILogger<MyConsumer> _logger;

    public MyConsumer(ILogger<MyConsumer> logger)
    {
        _logger = logger;
    }

    // 消费
    public async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
        await Task.CompletedTask;
    }

    // 每次失败时被执行，或者出现无法进入 ExecuteAsync 的异常
    public async Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
    {
        await Task.CompletedTask;
    }

    // 最后一次失败时执行
    public async Task<bool> FallbackAsync(EventBody<TestEvent>? message)
    {
        await Task.CompletedTask;
        return true;
    }
}
