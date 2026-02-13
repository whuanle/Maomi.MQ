using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/quickstart")]
public sealed class QuickStartController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public QuickStartController(IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _publisher = publisher;
        _state = state;
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
        _state.AddLog($"quickstart published: {message.Id}");
        return Results.Ok(message);
    }
}

public sealed class PublishQuickStartRequest
{
    public string Text { get; set; } = "hello quickstart";
}
