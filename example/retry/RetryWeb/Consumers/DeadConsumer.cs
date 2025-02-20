using Maomi.MQ;
using RetryWeb.Models;

namespace RetryWeb.Consumers;

[Consumer("RetryWeb", Qos = 5, RetryFaildRequeue = true)]
public class DeadConsumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
        throw new OperationCanceledException();
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
    {
        Console.WriteLine($"{message?.Id} 重试 {retryCount}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        return Task.FromResult(ConsumerState.Nack);
    }
}