using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/batch")]
public sealed class BatchController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public BatchController(IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _publisher = publisher;
        _state = state;
    }

    [HttpPost("publish-once")]
    public async Task<IResult> PublishOnce([FromBody] PublishBatchRequest request)
    {
        var count = request.Count <= 0 ? 10 : request.Count;
        var list = new List<MetricMessage>(count);

        for (var i = 0; i < count; i++)
        {
            var message = new MetricMessage
            {
                Value = Random.Shared.Next(1, 5000),
                At = DateTimeOffset.UtcNow.AddMilliseconds(i)
            };

            list.Add(message);
            await _publisher.AutoPublishAsync(message);
        }

        Interlocked.Add(ref _state.BatchPublished, list.Count);
        _state.AddLog($"batch publish-once count={list.Count}");
        return Results.Ok(new { Count = list.Count });
    }

    [HttpPost("worker/start")]
    public IResult StartWorker()
    {
        _state.BatchPublisherEnabled = true;
        _state.AddLog("batch worker started");
        return Results.Ok(new { Enabled = true });
    }

    [HttpPost("worker/stop")]
    public IResult StopWorker()
    {
        _state.BatchPublisherEnabled = false;
        _state.AddLog("batch worker stopped");
        return Results.Ok(new { Enabled = false });
    }
}

public sealed class PublishBatchRequest
{
    public int Count { get; set; } = 10;
}
