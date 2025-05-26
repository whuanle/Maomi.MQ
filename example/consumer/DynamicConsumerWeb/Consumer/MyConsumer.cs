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
    public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        throw new NotImplementedException();
    }

    // 每次失败时被执行，或者出现无法进入 ExecuteAsync 的异常
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
    {
        throw new NotImplementedException();
    }

    // 最后一次失败时执行
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        throw new NotImplementedException();
    }
}
