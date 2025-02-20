using Maomi.MQ;
using Maomi.MQ.Pool;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PublisherWeb.MQ;

[Consumer("publish", Qos = 1, RetryFaildRequeue = true)]
public class TestEventConsumer : IConsumer<TestMessageEvent>
{
    private static int _retryCount = 0;

    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, TestMessageEvent message)
    {
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessageEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessageEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}

public class MyDefaultBreakdown : IBreakdown
{
    private readonly ConnectionPool _connectionPool;

    public MyDefaultBreakdown(ConnectionPool connectionPool)
    {
        _connectionPool = connectionPool;
    }

    /// <inheritdoc />
    public async Task BasicReturnAsync(object sender, BasicReturnEventArgs @event)
    {
        var connectionObject = _connectionPool.Get();
        await connectionObject.DefaultChannel.BasicPublishAsync<BasicProperties>(
            @event.Exchange, 
            @event.RoutingKey + ".faild", 
            true, 
            new BasicProperties(@event.BasicProperties), 
            @event.Body);
    }

    /// <inheritdoc />
    public Task NotFoundConsumerAsync(string queue, Type messageType, Type consumerType)
    {
        return Task.CompletedTask;
    }
}

[Consumer("publish_faild", Qos = 1, RetryFaildRequeue = true)]
public class TestEventFaildConsumer : IConsumer<TestMessageEvent>
{
    private static int _retryCount = 0;

    // 消费
    public virtual async Task ExecuteAsync(MessageHeader messageHeader, TestMessageEvent message)
    {
        _retryCount++;
        Console.WriteLine($"执行次数:{_retryCount} 事件 id: {message.Id} {DateTime.Now}");
        await Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessageEvent message) => Task.CompletedTask;

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessageEvent? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
}
