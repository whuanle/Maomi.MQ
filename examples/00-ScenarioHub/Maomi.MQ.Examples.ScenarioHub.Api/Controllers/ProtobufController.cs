using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/protobuf")]
public sealed class ProtobufController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ProtobufController> _logger;

    public ProtobufController(IMessagePublisher publisher, ILogger<ProtobufController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishProtobufRequest request)
    {
        var message = new PersonMessage
        {
            Name = request.Name,
            Age = request.Age
        };

        await _publisher.AutoPublishAsync(message);
        _logger.LogInformation("Protobuf published. MessageId={MessageId}, Name={Name}", message.Id, message.Name);
        return Results.Ok(message);
    }
}

public sealed class PublishProtobufRequest
{
    public string Name { get; set; } = "protobuf-user";

    public int Age { get; set; } = 28;
}
