using Maomi.MQ;

[Consumer("opentelemetry_console", Qos = 100, BindExchange = "o1", ExchangeType = "fanout", RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"{DateTime.Now} consumer1[{messageHeader.Id}] event id: {message.Id} ");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("opentelemetry_console2", Qos = 100, BindExchange = "o1", ExchangeType = "fanout", RetryFaildRequeue = true)]
public class MyConsumer2 : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"{DateTime.Now} consumer2[{messageHeader.Id}] event id: {message.Id} ");
        await Task.CompletedTask;
    }


    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("opentelemetry_console3", Qos = 100, BindExchange = "o1", ExchangeType = "fanout", RetryFaildRequeue = true)]
public class MyConsumer3 : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        if (new Random().Next(0, 100) % 2 == 0)
        {
            Console.WriteLine($"{DateTime.Now} consumer3[{messageHeader.Id}] error");
            throw new Exception("1");
        }
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"{DateTime.Now} consumer3[{messageHeader.Id}] event id: {message.Id} ");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}



[Consumer("opentelemetry_console4", Qos = 100, BindExchange = "o1", ExchangeType = "fanout", RetryFaildRequeue = false)]
public class MyConsumer4 : IConsumer<TestEvent>
{
    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        Console.WriteLine($"{DateTime.Now} consumer4[{messageHeader.Id}] error");
        throw new Exception("1");
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}
