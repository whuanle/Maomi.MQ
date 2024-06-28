using Maomi.MQ;

[Consumer("opentelemetry_console", Qos = 1, RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"event id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    // 补偿
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}
