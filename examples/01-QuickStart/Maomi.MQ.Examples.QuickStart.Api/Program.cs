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
        options.WorkId = 1;
        options.AppName = "quickstart-api";
        options.Rabbit = rabbit =>
        {
            rabbit.Uri = new Uri(builder.Configuration["RabbitMQ"]!);
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

[RouterKey("example.quickstart")]
public sealed class QuickStartMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Text { get; set; } = string.Empty;
}

[Consumer("example.quickstart")]
public sealed class QuickStartConsumer : IConsumer<QuickStartMessage>
{
    private readonly ILogger<QuickStartConsumer> _logger;

    public QuickStartConsumer(ILogger<QuickStartConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, QuickStartMessage message)
    {
        _logger.LogInformation(
            "Consumed quickstart message. HeaderId={HeaderId}, MessageId={MessageId}, Text={Text}",
            messageHeader.Id,
            message.Id,
            message.Text);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, QuickStartMessage message)
    {
        _logger.LogWarning(
            ex,
            "Quickstart consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, QuickStartMessage? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "Quickstart fallback. HeaderId={HeaderId}, MessageId={MessageId}",
            messageHeader.Id,
            message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[ApiController]
[Route("api/quickstart")]
public sealed class QuickStartController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public QuickStartController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishQuickStartRequest request)
    {
        var message = new QuickStartMessage
        {
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _publisher.AutoPublishAsync(message);
        return Results.Ok(message);
    }
}

public sealed class PublishQuickStartRequest
{
    public string Text { get; set; } = "hello from quickstart";
}
