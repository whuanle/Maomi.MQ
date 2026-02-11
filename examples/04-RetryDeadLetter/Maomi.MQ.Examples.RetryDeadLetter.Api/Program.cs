using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        options.WorkId = 4;
        options.AppName = "retry-deadletter-api";
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

[QueueName("example.retry.main")]
public sealed class RetryMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public bool ForceFail { get; set; } = true;
}

[QueueName("example.retry.dead")]
public sealed class RetryDeadMessage
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset DeadAt { get; set; } = DateTimeOffset.UtcNow;
}

[Consumer(
    "example.retry.main",
    Qos = 1,
    RetryFaildRequeue = false,
    DeadExchange = "",
    DeadRoutingKey = "example.retry.dead")]
public sealed class RetryConsumer : IConsumer<RetryMessage>
{
    private readonly ILogger<RetryConsumer> _logger;
    private readonly IMessagePublisher _publisher;

    public RetryConsumer(ILogger<RetryConsumer> logger, IMessagePublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryMessage message)
    {
        _logger.LogInformation(
            "Retry consumer execute. HeaderId={HeaderId}, MessageId={MessageId}, ForceFail={ForceFail}",
            messageHeader.Id,
            message.Id,
            message.ForceFail);

        if (message.ForceFail)
        {
            throw new InvalidOperationException("This message is intentionally failed for retry/dead-letter demo.");
        }

        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryMessage message)
    {
        _logger.LogWarning(
            ex,
            "Retry attempt failed. HeaderId={HeaderId}, RetryCount={RetryCount}, MessageId={MessageId}",
            messageHeader.Id,
            retryCount,
            message.Id);
        return Task.CompletedTask;
    }

    public async Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Retry fallback reached. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);

        if (message != null)
        {
            await _publisher.PublishAsync(
                exchange: string.Empty,
                routingKey: "example.retry.dead",
                message: new RetryDeadMessage
                {
                    Id = message.Id,
                    Text = message.Text,
                    DeadAt = DateTimeOffset.UtcNow
                });
        }

        return ConsumerState.Ack;
    }
}

[Consumer("example.retry.dead", Qos = 1)]
public sealed class RetryDeadConsumer : IConsumer<RetryDeadMessage>
{
    private readonly ILogger<RetryDeadConsumer> _logger;

    public RetryDeadConsumer(ILogger<RetryDeadConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, RetryDeadMessage message)
    {
        _logger.LogInformation(
            "Dead-letter consumed. HeaderId={HeaderId}, MessageId={MessageId}, DeadAt={DeadAt}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.DeadAt,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, RetryDeadMessage message)
    {
        _logger.LogWarning(
            ex,
            "Dead-letter consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, RetryDeadMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Dead-letter fallback. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[ApiController]
[Route("api/retry")]
public sealed class RetryController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public RetryController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishRetryMessageRequest request)
    {
        var message = new RetryMessage
        {
            Text = request.Text,
            ForceFail = request.ForceFail
        };

        await _publisher.AutoPublishAsync(message);
        return Results.Ok(message);
    }
}

public sealed class PublishRetryMessageRequest
{
    public string Text { get; set; } = "retry me";

    public bool ForceFail { get; set; } = true;
}
