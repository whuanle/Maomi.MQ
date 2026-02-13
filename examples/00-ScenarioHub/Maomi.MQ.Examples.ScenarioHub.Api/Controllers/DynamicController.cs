using Maomi.MQ;
using Maomi.MQ.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace Maomi.MQ.Samples.ScenarioHub.Controllers;

[ApiController]
[Route("api/scenario/dynamic")]
public sealed class DynamicController : ControllerBase
{
    private readonly IDynamicConsumer _dynamicConsumer;
    private readonly IMessagePublisher _publisher;
    private readonly ScenarioRuntimeState _state;

    public DynamicController(IDynamicConsumer dynamicConsumer, IMessagePublisher publisher, ScenarioRuntimeState state)
    {
        _dynamicConsumer = dynamicConsumer;
        _publisher = publisher;
        _state = state;
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
                Interlocked.Increment(ref _state.DynamicConsumed);
                _state.AddLog($"dynamic consumed queue={queueName} id={message.Id}");
                await Task.CompletedTask;
            },
            faild: async (header, ex, retryCount, message) =>
            {
                _state.AddLog($"dynamic failed queue={queueName} id={message.Id} retry={retryCount} ex={ex.Message}");
                await Task.CompletedTask;
            },
            fallback: (header, message, ex) =>
            {
                _state.AddLog($"dynamic fallback queue={queueName} id={message?.Id} ex={ex?.Message}");
                return Task.FromResult(ConsumerState.Ack);
            });

        _state.DynamicConsumerTags[queueName] = tag;
        Interlocked.Increment(ref _state.DynamicStarted);
        _state.AddLog($"dynamic started queue={queueName} tag={tag}");

        return Results.Ok(new { Queue = queueName, ConsumerTag = tag });
    }

    [HttpDelete("stop/{queue}")]
    public async Task<IResult> Stop(string queue)
    {
        await _dynamicConsumer.StopConsumerAsync(queue);
        _state.DynamicConsumerTags.TryRemove(queue, out _);
        Interlocked.Increment(ref _state.DynamicStopped);
        _state.AddLog($"dynamic stopped queue={queue}");
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
        _state.AddLog($"dynamic published queue={request.Queue} id={message.Id}");
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
