using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;

// ConsumerWeb_dead 消费失败的消息会被此消费者消费。
[Consumer("consumerWeb_dead_queue", Qos = 1)]
public class Dead_QueueConsumer : IConsumer<DeadQueueEvent>
{
    // 消费
    public Task ExecuteAsync(MessageHeader messageHeader, DeadQueueEvent message)
    {
        Console.WriteLine($"死信队列，事件 id:{message.Id}");
        return Task.CompletedTask;
    }

    // 每次失败时被执行
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, DeadQueueEvent message) => Task.CompletedTask;

    // 最后一次失败时执行
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, DeadQueueEvent? message, Exception? ex)
        => Task.FromResult(ConsumerState.Ack);
}