using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using Maomi.MQ.Examples.EventBus.Api.Messages;

namespace Maomi.MQ.Examples.EventBus.Api.Consumers;

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
        _logger.LogInformation("[1] Reserve inventory for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[1] Cancel inventory reserve for OrderId={OrderId}", message.OrderId);
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
        _logger.LogInformation("[2] Create bill for OrderId={OrderId}, Amount={Amount}", message.OrderId, message.Amount);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[2] Cancel bill for OrderId={OrderId}", message.OrderId);
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
        _logger.LogInformation("[3] Notify customer={Customer} for OrderId={OrderId}", message.Customer, message.OrderId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[3] Cancel customer notify for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}
