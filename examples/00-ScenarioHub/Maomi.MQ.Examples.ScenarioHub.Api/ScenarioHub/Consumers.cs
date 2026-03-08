using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.Samples.ScenarioHub;

[Consumer("scenario.quickstart")]
public sealed class QuickStartConsumer : IConsumer<QuickStartMessage>
{
    private readonly ILogger<QuickStartConsumer> _logger;

    public QuickStartConsumer(ILogger<QuickStartConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, QuickStartMessage message)
    {
        _logger.LogInformation(
            "QuickStart consumed. HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, QuickStartMessage message)
    {
        _logger.LogWarning(
            ex,
            "QuickStart consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, QuickStartMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "QuickStart fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.eventbus.order")]
public sealed class OrderCreatedMiddleware : IEventMiddleware<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedMiddleware> _logger;

    public OrderCreatedMiddleware(ILogger<OrderCreatedMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, OrderCreatedEvent message, EventHandlerDelegate<OrderCreatedEvent> next)
    {
        _logger.LogInformation(
            "EventBus middleware start. HeaderId={HeaderId}, OrderId={OrderId}",
            messageHeader.Id,
            message.OrderId);
        await next(messageHeader, message, CancellationToken.None);
        _logger.LogInformation(
            "EventBus middleware end. HeaderId={HeaderId}, OrderId={OrderId}",
            messageHeader.Id,
            message.OrderId);
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, OrderCreatedEvent? message)
    {
        _logger.LogWarning(
            ex,
            "EventBus middleware failed. HeaderId={HeaderId}, OrderId={OrderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message?.OrderId,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, OrderCreatedEvent? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "EventBus middleware fallback reached. HeaderId={HeaderId}, OrderId={OrderId}",
            messageHeader.Id,
            message?.OrderId);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[EventOrder(1)]
public sealed class ReserveInventoryHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<ReserveInventoryHandler> _logger;

    public ReserveInventoryHandler(ILogger<ReserveInventoryHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventBus handler[1] reserve inventory. OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventBus handler[1] cancel inventory. OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[EventOrder(2)]
public sealed class CreateBillHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<CreateBillHandler> _logger;

    public CreateBillHandler(ILogger<CreateBillHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "EventBus handler[2] create bill. OrderId={OrderId}, Amount={Amount}",
            message.OrderId,
            message.Amount);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventBus handler[2] cancel bill. OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[EventOrder(3)]
public sealed class NotifyCustomerHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<NotifyCustomerHandler> _logger;

    public NotifyCustomerHandler(ILogger<NotifyCustomerHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "EventBus handler[3] notify customer. OrderId={OrderId}, Customer={Customer}",
            message.OrderId,
            message.Customer);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventBus handler[3] cancel notify. OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[Consumer(
    "scenario.retry.main",
    Qos = 1,
    RetryFaildRequeue = false,
    DeadExchange = "",
    DeadRoutingKey = "scenario.retry.dead")]
public sealed class RetryConsumer : IConsumer<RetryMessage>
{
    private readonly ILogger<RetryConsumer> _logger;
    private readonly IMessagePublisher _publisher;

    public RetryConsumer(ILogger<RetryConsumer> logger, IMessagePublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryMessage message)
    {
        if (message.ForceFail)
        {
            throw new InvalidOperationException("forced failure for retry demo");
        }

        _logger.LogInformation(
            "Retry consumed success. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message.Id);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryMessage message)
    {
        _logger.LogWarning(
            ex,
            "Retry consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Retry fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);

        if (message != null)
        {
            await _publisher.PublishAsync(
                exchange: string.Empty,
                routingKey: "scenario.retry.dead",
                message: new RetryDeadMessage
                {
                    Id = message.Id,
                    Text = message.Text,
                    DeadAt = DateTimeOffset.UtcNow
                });
        }

        return ConsumerState.Ack;
    }
}

[Consumer("scenario.retry.dead", Qos = 1)]
public sealed class RetryDeadConsumer : IConsumer<RetryDeadMessage>
{
    private readonly ILogger<RetryDeadConsumer> _logger;

    public RetryDeadConsumer(ILogger<RetryDeadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryDeadMessage message)
    {
        _logger.LogInformation(
            "Dead-letter consumed. HeaderId={HeaderId}, MessageId={MessageId}, DeadAt={DeadAt}",
            messageHeader.Id,
            message.Id,
            message.DeadAt);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryDeadMessage message)
    {
        _logger.LogWarning(
            ex,
            "Dead-letter consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryDeadMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Dead-letter fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.protobuf.person", Qos = 5)]
public sealed class PersonMessageConsumer : IConsumer<PersonMessage>
{
    private readonly ILogger<PersonMessageConsumer> _logger;

    public PersonMessageConsumer(ILogger<PersonMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, PersonMessage message)
    {
        _logger.LogInformation(
            "Protobuf consumed. HeaderId={HeaderId}, MessageId={MessageId}, Name={Name}, Age={Age}",
            messageHeader.Id,
            message.Id,
            message.Name,
            message.Age);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, PersonMessage message)
    {
        _logger.LogWarning(
            ex,
            "Protobuf consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, PersonMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Protobuf fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.batch.metrics", Qos = 20)]
public sealed class MetricConsumer : IConsumer<MetricMessage>
{
    private readonly ILogger<MetricConsumer> _logger;

    public MetricConsumer(ILogger<MetricConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, MetricMessage message)
    {
        _logger.LogInformation(
            "Batch metric consumed. HeaderId={HeaderId}, MessageId={MessageId}, Value={Value}",
            messageHeader.Id,
            message.Id,
            message.Value);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, MetricMessage message)
    {
        _logger.LogWarning(
            ex,
            "Batch metric consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, MetricMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Batch metric fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer(
    "scenario.broadcast.notice",
    BindExchange = "scenario.broadcast.exchange",
    ExchangeType = ExchangeType.Fanout,
    RoutingKey = "scenario.broadcast.notice",
    IsBroadcast = true,
    Qos = 1)]
public sealed class BroadcastConsumerAlpha1 : IConsumer<BroadcastNoticeMessage>
{
    private readonly ILogger<BroadcastConsumerAlpha1> _logger;

    public BroadcastConsumerAlpha1(ILogger<BroadcastConsumerAlpha1> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, BroadcastNoticeMessage message)
    {
        _logger.LogInformation(
            "Broadcast alpha consumed. HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, BroadcastNoticeMessage message)
    {
        _logger.LogWarning(
            ex,
            "Broadcast alpha consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, BroadcastNoticeMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Broadcast alpha fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer(
    "scenario.broadcast.notice",
    BindExchange = "scenario.broadcast.exchange",
    ExchangeType = ExchangeType.Fanout,
    RoutingKey = "scenario.broadcast.notice",
    IsBroadcast = true,
    Qos = 1)]
public sealed class BroadcastConsumerAlpha2 : IConsumer<BroadcastNoticeMessage>
{
    private readonly ILogger<BroadcastConsumerAlpha2> _logger;

    public BroadcastConsumerAlpha2(ILogger<BroadcastConsumerAlpha2> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, BroadcastNoticeMessage message)
    {
        _logger.LogInformation(
            "Broadcast alpha consumed. HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, BroadcastNoticeMessage message)
    {
        _logger.LogWarning(
            ex,
            "Broadcast alpha consume failed. HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
            messageHeader.Id,
            message.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, BroadcastNoticeMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Broadcast alpha fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}