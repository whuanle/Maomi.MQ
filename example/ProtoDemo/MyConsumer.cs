using Maomi.MQ;
using ProtoDemo;
using ProtoDemo.Proto;

[Consumer("proto_console1", Qos = 10000, BindExchange = "o2", ExchangeType = "fanout", RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static volatile int _retryCount = 0;

    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        var count = Interlocked.Increment(ref _retryCount);
        Console.WriteLine($"{DateTime.Now} json [{messageHeader.Id}] event id: {message.Id} ");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}


[Consumer("proto_console2", Qos = 10000, BindExchange = "o2", ExchangeType = "fanout", RetryFaildRequeue = false)]
public class MyConsumer4 : IConsumer<Person>
{
    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, Person message)
    {
        Console.WriteLine($"{DateTime.Now} proto [{messageHeader.Id}] event id: {message.Id} ");
        //throw new Exception("1");
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, Person message) => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, Person? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}