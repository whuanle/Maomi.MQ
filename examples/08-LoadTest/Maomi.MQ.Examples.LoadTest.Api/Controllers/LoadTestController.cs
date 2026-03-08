using System.Diagnostics;
using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.LoadTest.Controllers;

[ApiController]
[Route("api/loadtest")]
public sealed class LoadTestController : ControllerBase
{
    private const int DefaultCount = 1_000_000;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<LoadTestController> _logger;

    public LoadTestController(IMessagePublisher publisher, ILogger<LoadTestController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishLoadTestRequest? request, CancellationToken cancellationToken)
    {
        var queueNo = request?.QueueNo ?? 1;
        var count = request?.Count > 0 ? request.Count : DefaultCount;
        var queueName = ResolveQueueName(queueNo);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("LoadTest publish started. QueueNo={QueueNo}, QueueName={QueueName}, Count={Count}", queueNo, queueName, count);

        for (var i = 0; i < count; i++)
        {
            var sequence = i + 1L;
            var now = DateTimeOffset.UtcNow;
            var payload = $"load-{queueNo}-{sequence}";

            switch (queueNo)
            {
                case 1:
                    await _publisher.PublishAsync(string.Empty, LoadTestRoutes.Json, new JsonLoadMessage
                    {
                        Sequence = sequence,
                        At = now,
                        Payload = payload
                    }, cancellationToken: cancellationToken);
                    break;
                case 2:
                    await _publisher.PublishAsync(string.Empty, LoadTestRoutes.ProtobufNet, new ProtobufNetLoadMessage
                    {
                        Sequence = sequence,
                        UnixTimeMilliseconds = now.ToUnixTimeMilliseconds(),
                        Payload = payload
                    }, cancellationToken: cancellationToken);
                    break;
                case 3:
                    await _publisher.PublishAsync(string.Empty, LoadTestRoutes.MessagePack, new MessagePackLoadMessage
                    {
                        Sequence = sequence,
                        UnixTimeMilliseconds = now.ToUnixTimeMilliseconds(),
                        Payload = payload
                    }, cancellationToken: cancellationToken);
                    break;
                case 4:
                    await _publisher.PublishAsync(string.Empty, LoadTestRoutes.RawBinary, new RawBinaryLoadMessage
                    {
                        Sequence = sequence,
                        UnixTimeMilliseconds = now.ToUnixTimeMilliseconds(),
                        Payload = payload
                    }, cancellationToken: cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(queueNo), queueNo, "QueueNo must be 1, 2, 3 or 4.");
            }
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "LoadTest publish finished. QueueNo={QueueNo}, QueueName={QueueName}, Count={Count}, ElapsedMs={ElapsedMs}",
            queueNo,
            queueName,
            count,
            stopwatch.ElapsedMilliseconds);

        return Results.Ok(new
        {
            QueueNo = queueNo,
            QueueName = queueName,
            Count = count,
            ElapsedMs = stopwatch.ElapsedMilliseconds
        });
    }

    private static string ResolveQueueName(int queueNo)
    {
        return queueNo switch
        {
            1 => LoadTestRoutes.Json,
            2 => LoadTestRoutes.ProtobufNet,
            3 => LoadTestRoutes.MessagePack,
            4 => LoadTestRoutes.RawBinary,
            _ => throw new ArgumentOutOfRangeException(nameof(queueNo), queueNo, "QueueNo must be 1, 2, 3 or 4.")
        };
    }
}

public sealed class PublishLoadTestRequest
{
    public int QueueNo { get; set; } = 1;

    public int Count { get; set; } = 1_000_000;
}
