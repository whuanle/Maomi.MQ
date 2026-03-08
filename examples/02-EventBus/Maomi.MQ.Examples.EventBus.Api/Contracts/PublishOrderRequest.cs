namespace Maomi.MQ.Examples.EventBus.Api.Contracts;

public sealed class PublishOrderRequest
{
    public decimal Amount { get; set; } = 88.8m;

    public string Customer { get; set; } = "demo-customer";
}
