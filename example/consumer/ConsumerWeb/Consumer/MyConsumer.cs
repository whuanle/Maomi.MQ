using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;

[Consumer("ConsumerWeb", Qos = 1)]
public class MyConsumer : IConsumer<TestEvent>
{
    private readonly ILogger<MyConsumer> _logger;

    public MyConsumer(ILogger<MyConsumer> logger)
    {
        _logger = logger;
    }

    // 消费
    public async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
    }

    // 每次失败时被执行，或者出现无法进入 ExecuteAsync 的异常
    public async Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
    {
        // 当 retryCount == -1 时，错误并非是 ExecuteAsync 方法导致的
        if (retryCount == -1)
        {
            _logger.LogError(ex, "Consumer error,event id: {Id}", message?.Id);

            // 可以在此处添加告警通知代码
            await Task.Delay(1000);
        }
        else
        {
            _logger.LogError(ex, "Consumer exception,event id: {Id},retry count: {retryCount}", message!.Id, retryCount);
        }
    }


    // 最后一次失败时执行
    public async Task<bool> FallbackAsync(EventBody<TestEvent>? message)
    {
        return true;
    }
}

[Consumer("ConsumerWeb_dead", Qos = 1, DeadQueue = "ConsumerWeb_dead_queue", RetryFaildRequeue = false)]
public class DeadConsumer : IConsumer<DeadEvent>
{
    // 消费
    public Task ExecuteAsync(EventBody<DeadEvent> message)
    {
        Console.WriteLine($"事件 id:{message.Id}");
        throw new OperationCanceledException();
    }

    // 每次失败时被执行
    public Task FaildAsync(Exception ex, int retryCount, EventBody<DeadEvent>? message) => Task.CompletedTask;

    // 最后一次失败时执行
    public Task<bool> FallbackAsync(EventBody<DeadEvent>? message) => Task.FromResult(false);
}

// ConsumerWeb_dead 消费失败的消息会被此消费者消费。
[Consumer("ConsumerWeb_dead_queue", Qos = 1)]
public class DeadQueueConsumer : IConsumer<DeadQueueEvent>
{
    // 消费
    public Task ExecuteAsync(EventBody<DeadQueueEvent> message)
    {
        Console.WriteLine($"死信队列，事件 id:{message.Id}");
        return Task.CompletedTask;
    }

    // 每次失败时被执行
    public Task FaildAsync(Exception ex, int retryCount, EventBody<DeadQueueEvent>? message) => Task.CompletedTask;

    // 最后一次失败时执行
    public Task<bool> FallbackAsync(EventBody<DeadQueueEvent>? message) => Task.FromResult(false);
}

[Consumer("ConsumerWeb_dead_2", Expiration = 6000, DeadQueue = "ConsumerWeb_dead_queue_2")]
public class EmptyDeadConsumer : EmptyConsumer<DeadEvent>
{
}

// ConsumerWeb_dead 消费失败的消息会被此消费者消费。
[Consumer("ConsumerWeb_dead_queue_2", Qos = 1)]
public class Dead_2_QueueConsumer : IConsumer<DeadQueueEvent>
{
    // 消费
    public Task ExecuteAsync(EventBody<DeadQueueEvent> message)
    {
        Console.WriteLine($"死信队列，事件 id:{message.Id}");
        return Task.CompletedTask;
    }

    // 每次失败时被执行
    public Task FaildAsync(Exception ex, int retryCount, EventBody<DeadQueueEvent>? message) => Task.CompletedTask;

    // 最后一次失败时执行
    public Task<bool> FallbackAsync(EventBody<DeadQueueEvent>? message) => Task.FromResult(false);
}

[Consumer("ConsumerWeb_empty", Expiration = 6000, DeadQueue = "ConsumerWeb_empty_dead")]
public class MyEmptyConsumer : EmptyConsumer<TestEvent> { }

[Consumer("ConsumerWeb_empty_dead", Qos = 10)]
public class MyDeadConsumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[Consumer("ConsumerWeb_create", AutoQueueDeclare = AutoQueueDeclare.Enable)]
public class CreateConsumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[Consumer("ConsumerWeb_exchange_1", BindExchange = "exchange")]
public class Exchange_1_Consumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"[1]: {message.Id}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[Consumer("ConsumerWeb_exchange_2", BindExchange = "exchange")]
public class Exchange_2_Consumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"[2]: {message.Id}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}

[Consumer("ConsumerWeb_dynamic")]
public class DynamicConsumer : IConsumer<TestEvent>
{
    public Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Console.WriteLine($"[2]: {message.Id}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
}