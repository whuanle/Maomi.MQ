using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.EventBus.Api.Messages;

[RouterKey("example.eventbus")]
public sealed class OrderCreatedEvent
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public decimal Amount { get; set; }

    public string Customer { get; set; } = string.Empty;
}
