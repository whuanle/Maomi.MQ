using ActivitySourceApi.Models;
using Maomi.MQ;

namespace ActivitySourceApi.Consumer;

[Consumer("ActivitySourceApi", Qos = 1, RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;


    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        //throw new Exception();
        Console.WriteLine($"执行 {message.Id} 第几次：{_retryCount} {DateTime.Now}");
        _retryCount++;
        //await Task.Delay(1000);
    }

    // 每次失败时被执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
    {
        Console.WriteLine($"重试 {message.Id} 第几次：{retryCount} {DateTime.Now}");
        return Task.CompletedTask;
    }


    // 最后一次失败时执行

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        Console.WriteLine($"执行 {message.Id} 补偿 {DateTime.Now}");
        return Task.FromResult(ConsumerState.Ack);
    }
}
