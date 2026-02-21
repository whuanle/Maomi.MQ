using Maomi.MQ;
using Maomi.MQ.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/dynamic")]
public sealed class DynamicController : ControllerBase
{
    private readonly IDynamicConsumer _dynamicConsumer;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<DynamicController> _logger;

    public DynamicController(
        IDynamicConsumer dynamicConsumer,
        IMessagePublisher publisher,
        ILogger<DynamicController> logger)
    {
        _dynamicConsumer = dynamicConsumer;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IResult> Start([FromBody] StartDynamicConsumerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Queue))
        {
            return Results.BadRequest("queue is required");
        }

        var options = new ConsumerAttribute(request.Queue)
        {
            Qos = request.Qos <= 0 ? (ushort)1 : request.Qos
        };

        var queueName = request.Queue;

        var tag = await _dynamicConsumer.ConsumerAsync<DynamicMessage>(
            options,
            execute: async (header, message) =>
            {
                _logger.LogInformation(
                    "Dynamic consumed. Queue={Queue}, HeaderId={HeaderId}, MessageId={MessageId}",
                    queueName,
                    header.Id,
                    message.Id);
                await Task.CompletedTask;
            },
            faild: async (header, ex, retryCount, message) =>
            {
                _logger.LogWarning(
                    ex,
                    "Dynamic consume failed. Queue={Queue}, HeaderId={HeaderId}, MessageId={MessageId}, RetryCount={RetryCount}",
                    queueName,
                    header.Id,
                    message.Id,
                    retryCount);
                await Task.CompletedTask;
            },
            fallback: (header, message, ex) =>
            {
                _logger.LogError(
                    ex,
                    "Dynamic fallback reached. Queue={Queue}, HeaderId={HeaderId}, MessageId={MessageId}",
                    queueName,
                    header.Id,
                    message?.Id);
                return Task.FromResult(ConsumerState.Ack);
            });

        _logger.LogInformation("Dynamic consumer started. Queue={Queue}, ConsumerTag={Tag}", queueName, tag);

        return Results.Ok(new { Queue = queueName, ConsumerTag = tag });
    }

    [HttpDelete("stop/{queue}")]
    public async Task<IResult> Stop(string queue)
    {
        await _dynamicConsumer.StopConsumerAsync(queue);
        _logger.LogInformation("Dynamic consumer stopped. Queue={Queue}", queue);
        return Results.Ok(new { Queue = queue, Stopped = true });
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishDynamicRequest request)
    {
        var message = new DynamicMessage
        {
            Text = request.Text
        };

        await _publisher.PublishAsync(string.Empty, request.Queue, message);
        _logger.LogInformation(
            "Dynamic published. Queue={Queue}, MessageId={MessageId}, Text={Text}",
            request.Queue,
            message.Id,
            message.Text);
        return Results.Ok(new { request.Queue, Message = message });
    }
}

public sealed class StartDynamicConsumerRequest
{
    public string Queue { get; set; } = "scenario.dynamic.runtime";

    public ushort Qos { get; set; } = 1;
}

public sealed class PublishDynamicRequest
{
    public string Queue { get; set; } = "scenario.dynamic.runtime";

    public string Text { get; set; } = "hello dynamic";
}
