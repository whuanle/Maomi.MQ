using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/retry")]
public sealed class RetryDeadLetterController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public RetryDeadLetterController(IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _publisher = publisher;
        _state = state;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishRetryRequest request)
    {
        var message = new RetryMessage
        {
            Text = request.Text,
            ForceFail = request.ForceFail
        };

        await _publisher.AutoPublishAsync(message);
        _state.AddLog($"retry published: {message.Id} forceFail={message.ForceFail}");
        return Results.Ok(message);
    }
}

public sealed class PublishRetryRequest
{
    public string Text { get; set; } = "retry me";

    public bool ForceFail { get; set; } = true;
}
