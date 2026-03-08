using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/quickstart")]
public sealed class QuickStartController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<QuickStartController> _logger;

    public QuickStartController(IMessagePublisher publisher, ILogger<QuickStartController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishQuickStartRequest request)
    {
        var message = new QuickStartMessage
        {
            Text = request.Text,
            At = DateTimeOffset.UtcNow
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation("QuickStart published. MessageId={MessageId}, Text={Text}", message.Id, message.Text);
        return Results.Ok(message);
    }
}

public sealed class PublishQuickStartRequest
{
    public string Text { get; set; } = "hello quickstart";
}
