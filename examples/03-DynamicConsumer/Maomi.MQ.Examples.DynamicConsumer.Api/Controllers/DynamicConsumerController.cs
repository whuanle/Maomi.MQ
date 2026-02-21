using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Examples.DynamicConsumer.Api.Contracts;
using Maomi.MQ.Examples.DynamicConsumer.Api.Messages;
using Maomi.MQ.Examples.DynamicConsumer.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maomi.MQ.Examples.DynamicConsumer.Api.Controllers;

[ApiController]
[Route("api/dynamic")]
public sealed class DynamicConsumerController : ControllerBase
{
    private readonly IDynamicConsumer _dynamicConsumer;
    private readonly IMessagePublisher _publisher;
    private readonly DynamicConsumerRegistry _registry;
    private readonly ILogger<DynamicConsumerController> _logger;

    public DynamicConsumerController(
        IDynamicConsumer dynamicConsumer,
        IMessagePublisher publisher,
        DynamicConsumerRegistry registry,
        ILogger<DynamicConsumerController> logger)
    {
        _dynamicConsumer = dynamicConsumer;
        _publisher = publisher;
        _registry = registry;
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

        var consumerTag = await _dynamicConsumer.ConsumerAsync<DynamicMessage>(
            options,
            execute: async (header, message) =>
            {
                _logger.LogInformation(
                    "Dynamic consumer received. Queue={Queue}, HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
                    request.Queue,
                    header.Id,
                    message.Id,
                    message.Text);
                await Task.CompletedTask;
            },
            faild: async (header, ex, retryCount, message) =>
            {
                _logger.LogWarning(
                    ex,
                    "Dynamic consumer failed. Queue={Queue}, HeaderId={HeaderId}, RetryCount={RetryCount}",
                    request.Queue,
                    header.Id,
                    retryCount);
                await Task.CompletedTask;
            },
            fallback: (header, message, ex) =>
            {
                _logger.LogError(
                    ex,
                    "Dynamic consumer fallback. Queue={Queue}, HeaderId={HeaderId}, MessageId={MessageId}",
                    request.Queue,
                    header.Id,
                    message?.Id);
                return Task.FromResult(ConsumerState.Ack);
            });

        _registry.ConsumerTags[request.Queue] = consumerTag;
        return Results.Ok(new { request.Queue, ConsumerTag = consumerTag });
    }

    [HttpDelete("stop/{queue}")]
    public async Task<IResult> Stop(string queue)
    {
        await _dynamicConsumer.StopConsumerAsync(queue);
        _registry.ConsumerTags.TryRemove(queue, out _);
        return Results.Ok(new { queue, Stopped = true });
    }

    [HttpGet("list")]
    public IResult List()
    {
        var values = _registry.ConsumerTags
            .Select(x => new { Queue = x.Key, ConsumerTag = x.Value })
            .OrderBy(x => x.Queue)
            .ToArray();

        return Results.Ok(values);
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishDynamicMessageRequest request)
    {
        var message = new DynamicMessage
        {
            Text = request.Text
        };

        await _publisher.PublishAsync(string.Empty, request.Queue, message);
        return Results.Ok(new { request.Queue, Message = message });
    }
}
