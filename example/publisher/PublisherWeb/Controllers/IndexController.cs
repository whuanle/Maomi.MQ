using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PublisherWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        for (var i = 0; i < 100; i++)
        {
            await _messagePublisher.PublishAsync(queue: "publish", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }


    [HttpGet("publish_tran")]
    public async Task<string> Publisher_Tran()
    {
        using var tranPublisher = await _messagePublisher.TxSelectAsync();

        try
        {
            await tranPublisher.PublishAsync(queue: "publish_tran", message: new TestEvent
            {
                Id = 666
            });
            await Task.Delay(5000);
            await tranPublisher.TxCommitAsync();
        }
        catch
        {
            await tranPublisher.TxRollbackAsync();
            throw;
        }

        return "ok";
    }

    [HttpGet("publish_confirm")]
    public async Task<string> Publisher_Confirm()
    {
        using var confirmPublisher = await _messagePublisher.ConfirmSelectAsync();

        for (var i = 0; i < 5; i++)
        {
            await confirmPublisher.PublishAsync(queue: "publish_confirm1", message: new TestEvent
            {
                Id = 666
            });

            var result = await confirmPublisher.WaitForConfirmsAsync();

            // 如果在超时内没有接收到 nacks，则为 True，否则为 false。
            Console.WriteLine($"发布 {i},{result}");
        }

        return "ok";
    }
}

[Consumer("publish", Qos = 1, RetryFaildRequeue = true)]
public class MyConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;

    // 消费
    public virtual async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[Consumer("publish_tran", Qos = 1, RetryFaildRequeue = true)]
public class TranConsumer : MyConsumer
{
}

[Consumer("publish_confirm", Qos = 1, RetryFaildRequeue = true)]
public class ConfirmConsumer : IConsumer<TestEvent>
{
    private static int _retryCount = 0;
    // 消费
    public virtual async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        await Task.Delay(10000);
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
    }
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

public class TestEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}
