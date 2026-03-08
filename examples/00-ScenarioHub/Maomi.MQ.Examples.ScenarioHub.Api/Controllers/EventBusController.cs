using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/eventbus")]
public sealed class EventBusController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<EventBusController> _logger;

    public EventBusController(IMessagePublisher publisher, ILogger<EventBusController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishOrderRequest request)
    {
        var message = new OrderCreatedEvent
        {
            Amount = request.Amount,
            Customer = request.Customer
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation(
            "EventBus published. OrderId={OrderId}, Amount={Amount}, Customer={Customer}",
            message.OrderId,
            message.Amount,
            message.Customer);
        return Results.Ok(message);
    }
}

public sealed class PublishOrderRequest
{
    public decimal Amount { get; set; } = 99.9m;

    public string Customer { get; set; } = "scenario-customer";
}
