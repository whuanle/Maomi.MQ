using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/protobuf")]
public sealed class ProtobufController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public ProtobufController(IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _publisher = publisher;
        _state = state;
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
        _state.AddLog($"protobuf published: {message.Id} name={message.Name}");
        return Results.Ok(message);
    }
}

public sealed class PublishProtobufRequest
{
    public string Name { get; set; } = "protobuf-user";

    public int Age { get; set; } = 28;
}
