using Maomi.MQ;
using RetryWeb.Models;

namespace RetryWeb.Consumers;

[Consumer("RetryWeb", Qos = 5, RetryFaildRequeue = true)]
public class DeadConsumer : IConsumer<TestEvent>
{
    // 消费
    public Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
        throw new OperationCanceledException();
    }

    // 每次失败时被执行
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    // 最后一次失败时执行
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(false);
}