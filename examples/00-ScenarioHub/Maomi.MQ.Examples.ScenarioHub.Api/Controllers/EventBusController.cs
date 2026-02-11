using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/eventbus")]
public sealed class EventBusController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public EventBusController(IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _publisher = publisher;
        _state = state;
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
        _state.AddLog($"eventbus published: {message.OrderId}");
        return Results.Ok(message);
    }
}

public sealed class PublishOrderRequest
{
    public decimal Amount { get; set; } = 99.9m;

    public string Customer { get; set; } = "scenario-customer";
}
