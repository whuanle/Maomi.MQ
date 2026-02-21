using Maomi.MQ;
using Maomi.MQ.Examples.EventBus.Api.Contracts;
using Maomi.MQ.Examples.EventBus.Api.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Examples.EventBus.Api.Controllers;

[ApiController]
[Route("api/eventbus")]
public sealed class EventBusController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public EventBusController(IMessagePublisher publisher)
    {
        _publisher = publisher;
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
        return Results.Ok(message);
    }
}
