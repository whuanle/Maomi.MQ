using Maomi.MQ;
using Maomi.MQ.Examples.RetryDeadLetter.Api.Contracts;
using Maomi.MQ.Examples.RetryDeadLetter.Api.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Controllers;

[ApiController]
[Route("api/retry")]
public sealed class RetryController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public RetryController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishRetryMessageRequest request)
    {
        var message = new RetryMessage
        {
            Text = request.Text,
            ForceFail = request.ForceFail
        };

        await _publisher.AutoPublishAsync(message);
        return Results.Ok(message);
    }
}
