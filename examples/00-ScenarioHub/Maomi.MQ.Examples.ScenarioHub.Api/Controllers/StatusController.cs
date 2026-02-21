using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/status")]
public sealed class StatusController : ControllerBase
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;
    private readonly BatchPublisherBackgroundService _batchWorker;
    private readonly ILogger<StatusController> _logger;

    public StatusController(BatchPublisherBackgroundService batchWorker, ILogger<StatusController> logger)
    {
        _batchWorker = batchWorker;
        _logger = logger;
    }

    [HttpGet]
    public IResult GetStatus()
    {
        return Results.Ok(new
        {
            StartedAt,
            Now = DateTimeOffset.UtcNow,
            BatchPublisherEnabled = _batchWorker.IsEnabled
        });
    }

    [HttpPost("reset")]
    public IResult Reset()
    {
        _batchWorker.SetEnabled(false);
        _logger.LogInformation("Scenario status reset requested.");
        return Results.Ok(new { Reset = true, BatchPublisherEnabled = _batchWorker.IsEnabled });
    }
}
