using Maomi.MQ;
using Maomi.MQ.Pool;
using RabbitMQ.Client.Events;

namespace PublisherWeb.MQ;

public class FanoutEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}


[Consumer("fanout_1", BindExchange = "fanouttest",ExchangeType = "fanout")]
public class FanoutEvent_1_Consumer : IConsumer<FanoutEvent>
{
    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, FanoutEvent message)
    {
        Console.WriteLine($"【fanout_1】，事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, FanoutEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, FanoutEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("fanout_2", BindExchange = "fanouttest", ExchangeType = "fanout")]
public class FanoutEvent_2_Consumer : IConsumer<FanoutEvent>
{
    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, FanoutEvent message)
    {
        Console.WriteLine($"【fanout_2】，事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, FanoutEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, FanoutEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}