using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using Maomi.MQ.Examples.EventBus.Api.Messages;

namespace Maomi.MQ.Examples.EventBus.Api.Consumers;

[Consumer("example.eventbus")]
public sealed class OrderCreatedMiddleware : IEventMiddleware<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedMiddleware> _logger;

    public OrderCreatedMiddleware(ILogger<OrderCreatedMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, OrderCreatedEvent message, EventHandlerDelegate<OrderCreatedEvent> next)
    {
        _logger.LogInformation("EventBus start. HeaderId={HeaderId}, OrderId={OrderId}", messageHeader.Id, message.OrderId);
        await next(messageHeader, message, CancellationToken.None);
        _logger.LogInformation("EventBus end. HeaderId={HeaderId}, OrderId={OrderId}", messageHeader.Id, message.OrderId);
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, OrderCreatedEvent? message)
    {
        _logger.LogWarning(
            ex,
            "EventBus middleware failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, OrderCreatedEvent? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "EventBus middleware fallback. HeaderId={HeaderId}, OrderId={OrderId}",
            messageHeader.Id,
            message?.OrderId);
        return Task.FromResult(ConsumerState.Ack);
    }
}
