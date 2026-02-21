using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/batch")]
public sealed class BatchController : ControllerBase
{
    private readonly IMessagePublisher _publisher;
    private readonly BatchPublisherBackgroundService _batchWorker;
    private readonly ILogger<BatchController> _logger;

    public BatchController(
        IMessagePublisher publisher,
        BatchPublisherBackgroundService batchWorker,
        ILogger<BatchController> logger)
    {
        _publisher = publisher;
        _batchWorker = batchWorker;
        _logger = logger;
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

        _logger.LogInformation("Batch publish-once done. Count={Count}", list.Count);
        return Results.Ok(new { Count = list.Count });
    }

    [HttpPost("worker/start")]
    public IResult StartWorker()
    {
        _batchWorker.SetEnabled(true);
        return Results.Ok(new { Enabled = _batchWorker.IsEnabled });
    }

    [HttpPost("worker/stop")]
    public IResult StopWorker()
    {
        _batchWorker.SetEnabled(false);
        return Results.Ok(new { Enabled = _batchWorker.IsEnabled });
    }
}

public sealed class PublishBatchRequest
{
    public int Count { get; set; } = 10;
}
