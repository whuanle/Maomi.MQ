using Maomi.MQ;
using Maomi.MQ.Examples.QuickStart.Api.Contracts;
using Maomi.MQ.Examples.QuickStart.Api.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Examples.QuickStart.Api.Controllers;

[ApiController]
[Route("api/quickstart")]
public sealed class QuickStartController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<QuickStartController> _logger;

    public QuickStartController(IMessagePublisher publisher, ILogger<QuickStartController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpGet]
    public IResult GetApiInfo()
    {
        return Results.Ok(new
        {
            Name = "Maomi.MQ QuickStart API",
            Endpoints = new[]
            {
                "GET /api/quickstart/ping",
                "POST /api/quickstart/publish",
                "POST /api/quickstart/publish/simple?text=hello",
                "POST /api/quickstart/publish/batch"
            }
        });
    }

    [HttpGet("ping")]
    public IResult Ping()
    {
        return Results.Ok(new
        {
            Status = "ok",
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishQuickStartRequest request)
    {
        var message = new QuickStartMessage
        {
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation("QuickStart published. MessageId={MessageId}, Text={Text}", message.Id, message.Text);
        return Results.Ok(message);
    }

    [HttpPost("publish/simple")]
    public async Task<IResult> PublishSimple([FromQuery] string text = "hello from query")
    {
        var message = new QuickStartMessage
        {
            Text = text,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation("QuickStart simple published. MessageId={MessageId}, Text={Text}", message.Id, message.Text);

        return Results.Ok(message);
    }

    [HttpPost("publish/batch")]
    public async Task<IResult> PublishBatch([FromBody] PublishQuickStartBatchRequest request)
    {
        var count = request.Count <= 0 ? 1 : Math.Min(request.Count, 1000);
        var ids = new List<Guid>(count);
        var now = DateTimeOffset.UtcNow;

        for (var i = 1; i <= count; i++)
        {
            var message = new QuickStartMessage
            {
                Text = $"{request.TextPrefix} #{i}",
                CreatedAt = now
            };

            ids.Add(message.Id);
            await _publisher.AutoPublishAsync(message);
        }

        _logger.LogInformation("QuickStart batch published. Count={Count}, Prefix={Prefix}", count, request.TextPrefix);

        return Results.Ok(new
        {
            Count = count,
            MessageIds = ids
        });
    }
}
