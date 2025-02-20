using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;

[Consumer("consumer1", Qos = 1)]
public class MyConsumer : IConsumer<TestEvent>
{
    private readonly ILogger<MyConsumer> _logger;

    public MyConsumer(ILogger<MyConsumer> logger)
    {
        _logger = logger;
    }

    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
    }

    // 每次失败时被执行
    public async Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
    {
        _logger.LogError(ex, "Consumer exception,event id: {Id},retry count: {retryCount}", message!.Id, retryCount);
    }

    // 最后一次失败时执行
    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        return ConsumerState.Ack;
    }
}
