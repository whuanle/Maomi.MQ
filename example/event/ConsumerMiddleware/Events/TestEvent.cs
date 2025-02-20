using Maomi.MQ;
using Maomi.MQ.EventBus;
using Polly;

namespace ConsumerMiddleware.Events;

[EventTopic("web2")]
public class TestEvent
{
    public int Id { get; set; }
    public string Message { get; set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}


public class TestEventMiddleware : IEventMiddleware<TestEvent>
{
    private readonly BloggingContext _bloggingContext;

    public TestEventMiddleware(BloggingContext bloggingContext)
    {
        _bloggingContext = bloggingContext;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message, EventHandlerDelegate<TestEvent> next)
    {
        using (var transaction = _bloggingContext.Database.BeginTransaction())
        {
            await next(messageHeader, message, CancellationToken.None);
            await transaction.CommitAsync();
        }
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent? message)
    {
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        return Task.FromResult(ConsumerState.Ack);
    }
}

[EventOrder(0)]
public class My1EventEventHandler : IEventHandler<TestEvent>
{
    private readonly BloggingContext _bloggingContext;

    public My1EventEventHandler(BloggingContext bloggingContext)
    {
        _bloggingContext = bloggingContext;
    }

    public async Task CancelAsync(TestEvent message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Message} 被补偿,[1]");
    }

    public async Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken)
    {
        await _bloggingContext.Posts.AddAsync(new Post
        {
            Title = "鲁滨逊漂流记",
            Content = "随便写写就对了"
        });
        await _bloggingContext.SaveChangesAsync();
    }
}

[EventOrder(1)]
public class My2EventEventHandler : IEventHandler<TestEvent>
{
    private readonly BloggingContext _bloggingContext;

    public My2EventEventHandler(BloggingContext bloggingContext)
    {
        _bloggingContext = bloggingContext;
    }
    public async Task CancelAsync(TestEvent message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{message.Id} 被补偿,[2]");
    }

    public async Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken)
    {
        await _bloggingContext.Posts.AddAsync(new Post
        {
            Title = "红楼梦",
            Content = "贾宝玉初试云雨情"
        });
        await _bloggingContext.SaveChangesAsync();

        throw new OperationCanceledException("故意报错");
    }
}
