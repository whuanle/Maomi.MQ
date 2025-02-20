using Maomi.MQ;
using Microsoft.Extensions.Hosting;

namespace WebDemo.MQ;

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}

[Consumer("test")]
public class MyConsumer : IConsumer<TestEvent>
{
    // 消费
    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        Console.WriteLine($"事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    // 每次消费失败时执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
        => Task.CompletedTask;

    // 补偿
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
        => Task.FromResult(ConsumerState.Ack);
}

public class MyPublishAsync : BackgroundService
{
    private readonly IDynamicConsumer _dynamicConsumer;

    public MyPublishAsync(IDynamicConsumer dynamicConsumer)
    {
        _dynamicConsumer = dynamicConsumer;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {

            await Task.Delay(1000);
        }
    }
}