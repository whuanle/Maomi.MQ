using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<DynamicConsumerRegistry>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        options.WorkId = 3;
        options.AppName = "dynamic-consumer-api";
        options.Rabbit = rabbit =>
        {
            rabbit.HostName = builder.Configuration["RabbitMQ:Host"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ")
                ?? "127.0.0.1";
            rabbit.Port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
            rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            rabbit.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
        };
    },
    [typeof(Program).Assembly]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

[QueueName("example.dynamic.default")]
public sealed class DynamicMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;
}

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

public sealed class StartDynamicConsumerRequest
{
    public string Queue { get; set; } = "example.dynamic.runtime";

    public ushort Qos { get; set; } = 1;
}

public sealed class PublishDynamicMessageRequest
{
    public string Queue { get; set; } = "example.dynamic.runtime";

    public string Text { get; set; } = "hello dynamic";
}

public sealed class DynamicConsumerRegistry
{
    public ConcurrentDictionary<string, string> ConsumerTags { get; } = new(StringComparer.OrdinalIgnoreCase);
}
