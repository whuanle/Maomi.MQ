using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/status")]
public sealed class StatusController : ControllerBase
{
    private readonly ScenarioRuntimeState _state;

    public StatusController(ScenarioRuntimeState state)
    {
        _state = state;
    }

    [HttpGet]
    public IResult GetStatus()
    {
        return Results.Ok(_state.Snapshot());
    }

    [HttpPost("reset")]
    public IResult Reset()
    {
        _state.QuickStartConsumed = 0;
        _state.EventBusConsumed = 0;
        _state.DynamicConsumed = 0;
        _state.RetryConsumed = 0;
        _state.RetryFailed = 0;
        _state.DeadLetterConsumed = 0;
        _state.ProtobufConsumed = 0;
        _state.BatchPublished = 0;
        _state.BatchConsumed = 0;
        _state.DynamicStarted = 0;
        _state.DynamicStopped = 0;
        _state.BatchPublisherEnabled = false;

        _state.DynamicConsumerTags.Clear();

        while (_state.Logs.TryDequeue(out _))
        {
        }

        _state.AddLog("state reset");
        return Results.Ok(new { Reset = true });
    }
}
