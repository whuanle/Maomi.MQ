using Maomi.MQ;

namespace PublisherWeb.MQ;


public class TopicEvent
{
    public int Id { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}


[Consumer("red.yellow.#", BindExchange = "topictest", ExchangeType = "topic")]
public class TopicEvent_1_Consumer : IConsumer<TopicEvent>
{
    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, TopicEvent message)
    {
        Console.WriteLine($"【red.yellow.#】，事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TopicEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TopicEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

[Consumer("red.#", BindExchange = "topictest", ExchangeType = "topic")]
public class TopicEvent_2_Consumer : IConsumer<TopicEvent>
{
    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, TopicEvent message)
    {
        Console.WriteLine($"【red.#】，事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TopicEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TopicEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}
