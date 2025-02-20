using Maomi.MQ;

namespace PublisherWeb.MQ;

[Consumer("publish_confirm", Qos = 1, RetryFaildRequeue = true)]
public class ConfirmConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;
    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        await Task.Delay(10000);
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
    }
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}
