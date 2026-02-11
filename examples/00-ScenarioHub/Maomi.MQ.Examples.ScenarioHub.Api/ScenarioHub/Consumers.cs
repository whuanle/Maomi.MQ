using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using System.Threading;

namespace Maomi.MQ.Samples.ScenarioHub;

[Consumer("scenario.quickstart")]
public sealed class QuickStartConsumer : IConsumer<QuickStartMessage>
{
    private readonly ScenarioRuntimeState _state;

    public QuickStartConsumer(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, QuickStartMessage message)
    {
        Interlocked.Increment(ref _state.QuickStartConsumed);
        _state.AddLog($"quickstart consumed: {message.Id} text={message.Text}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, QuickStartMessage message)
    {
        _state.AddLog($"quickstart failed: {message.Id} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, QuickStartMessage? message, Exception? ex)
    {
        _state.AddLog($"quickstart fallback: {message?.Id} ex={ex?.Message}");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.eventbus.order")]
public sealed class OrderCreatedMiddleware : IEventMiddleware<OrderCreatedEvent>
{
    private readonly ScenarioRuntimeState _state;

    public OrderCreatedMiddleware(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, OrderCreatedEvent message, EventHandlerDelegate<OrderCreatedEvent> next)
    {
        Interlocked.Increment(ref _state.EventBusConsumed);
        _state.AddLog($"eventbus middleware start: {message.OrderId}");
        await next(messageHeader, message, CancellationToken.None);
        _state.AddLog($"eventbus middleware end: {message.OrderId}");
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, OrderCreatedEvent? message)
    {
        _state.AddLog($"eventbus middleware failed: {message?.OrderId} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, OrderCreatedEvent? message, Exception? ex)
    {
        _state.AddLog($"eventbus middleware fallback: {message?.OrderId} ex={ex?.Message}");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[EventOrder(1)]
public sealed class ReserveInventoryHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ScenarioRuntimeState _state;

    public ReserveInventoryHandler(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[1] reserve inventory: {message.OrderId}");
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[1] cancel inventory: {message.OrderId}");
        return Task.CompletedTask;
    }
}

[EventOrder(2)]
public sealed class CreateBillHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ScenarioRuntimeState _state;

    public CreateBillHandler(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[2] create bill: {message.OrderId} amount={message.Amount}");
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[2] cancel bill: {message.OrderId}");
        return Task.CompletedTask;
    }
}

[EventOrder(3)]
public sealed class NotifyCustomerHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ScenarioRuntimeState _state;

    public NotifyCustomerHandler(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[3] notify customer: {message.OrderId} customer={message.Customer}");
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _state.AddLog($"eventbus handler[3] cancel notify: {message.OrderId}");
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
    private readonly ScenarioRuntimeState _state;
    private readonly IMessagePublisher _publisher;

    public RetryConsumer(ScenarioRuntimeState state, IMessagePublisher publisher)
    {
        _state = state;
        _publisher = publisher;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryMessage message)
    {
        Interlocked.Increment(ref _state.RetryConsumed);

        if (message.ForceFail)
        {
            throw new InvalidOperationException("forced failure for retry demo");
        }

        _state.AddLog($"retry consumed success: {message.Id}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryMessage message)
    {
        Interlocked.Increment(ref _state.RetryFailed);
        _state.AddLog($"retry failed: {message.Id} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryMessage? message, Exception? ex)
    {
        _state.AddLog($"retry fallback: {message?.Id} ex={ex?.Message}");

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
    private readonly ScenarioRuntimeState _state;

    public RetryDeadConsumer(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryDeadMessage message)
    {
        Interlocked.Increment(ref _state.DeadLetterConsumed);
        _state.AddLog($"dead-letter consumed: {message.Id} at={message.DeadAt:O}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryDeadMessage message)
    {
        _state.AddLog($"dead-letter failed: {message.Id} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryDeadMessage? message, Exception? ex)
    {
        _state.AddLog($"dead-letter fallback: {message?.Id} ex={ex?.Message}");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.protobuf.person", Qos = 5)]
public sealed class PersonMessageConsumer : IConsumer<PersonMessage>
{
    private readonly ScenarioRuntimeState _state;

    public PersonMessageConsumer(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, PersonMessage message)
    {
        Interlocked.Increment(ref _state.ProtobufConsumed);
        _state.AddLog($"protobuf consumed: {message.Id} name={message.Name} age={message.Age}");
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, PersonMessage message)
    {
        _state.AddLog($"protobuf failed: {message.Id} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, PersonMessage? message, Exception? ex)
    {
        _state.AddLog($"protobuf fallback: {message?.Id} ex={ex?.Message}");
        return Task.FromResult(ConsumerState.Ack);
    }
}

[Consumer("scenario.batch.metrics", Qos = 20)]
public sealed class MetricConsumer : IConsumer<MetricMessage>
{
    private readonly ScenarioRuntimeState _state;

    public MetricConsumer(ScenarioRuntimeState state)
    {
        _state = state;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, MetricMessage message)
    {
        Interlocked.Increment(ref _state.BatchConsumed);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, MetricMessage message)
    {
        _state.AddLog($"batch metric failed: {message.Id} retry={retryCount} ex={ex.Message}");
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, MetricMessage? message, Exception? ex)
    {
        _state.AddLog($"batch metric fallback: {message?.Id} ex={ex?.Message}");
        return Task.FromResult(ConsumerState.Ack);
    }
}
