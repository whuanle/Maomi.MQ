using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/broadcast")]
public sealed class BroadcastController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<BroadcastController> _logger;

    public BroadcastController(IMessagePublisher publisher, ILogger<BroadcastController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] BroadcastNoticeMessage request)
    {
        var message = new BroadcastNoticeMessage
        {
            Text = request.Text,
            At = DateTimeOffset.UtcNow
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation("Broadcast published. MessageId={MessageId}, Text={Text}", message.Id, message.Text);
        return Results.Ok(message);
    }
}
