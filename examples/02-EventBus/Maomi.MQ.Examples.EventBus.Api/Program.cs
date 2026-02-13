using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
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
        options.WorkId = 2;
        options.AppName = "eventbus-api";
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

[RouterKey("example.eventbus")]
public sealed class OrderCreatedEvent
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public decimal Amount { get; set; }

    public string Customer { get; set; } = string.Empty;
}

[Consumer("example.eventbus")]
public sealed class OrderCreatedMiddleware : IEventMiddleware<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedMiddleware> _logger;

    public OrderCreatedMiddleware(ILogger<OrderCreatedMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, OrderCreatedEvent message, EventHandlerDelegate<OrderCreatedEvent> next)
    {
        _logger.LogInformation("EventBus start. HeaderId={HeaderId}, OrderId={OrderId}", messageHeader.Id, message.OrderId);
        await next(messageHeader, message, CancellationToken.None);
        _logger.LogInformation("EventBus end. HeaderId={HeaderId}, OrderId={OrderId}", messageHeader.Id, message.OrderId);
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, OrderCreatedEvent? message)
    {
        _logger.LogWarning(
            ex,
            "EventBus middleware failed. HeaderId={HeaderId}, RetryCount={RetryCount}",
            messageHeader.Id,
            retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, OrderCreatedEvent? message, Exception? ex)
    {
        _logger.LogError(
            ex,
            "EventBus middleware fallback. HeaderId={HeaderId}, OrderId={OrderId}",
            messageHeader.Id,
            message?.OrderId);
        return Task.FromResult(ConsumerState.Ack);
    }
}

[EventOrder(1)]
public sealed class ReserveInventoryHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<ReserveInventoryHandler> _logger;

    public ReserveInventoryHandler(ILogger<ReserveInventoryHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[1] Reserve inventory for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[1] Cancel inventory reserve for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[EventOrder(2)]
public sealed class CreateBillHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<CreateBillHandler> _logger;

    public CreateBillHandler(ILogger<CreateBillHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[2] Create bill for OrderId={OrderId}, Amount={Amount}", message.OrderId, message.Amount);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[2] Cancel bill for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[EventOrder(3)]
public sealed class NotifyCustomerHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<NotifyCustomerHandler> _logger;

    public NotifyCustomerHandler(ILogger<NotifyCustomerHandler> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[3] Notify customer={Customer} for OrderId={OrderId}", message.Customer, message.OrderId);
        return Task.CompletedTask;
    }

    public Task CancelAsync(OrderCreatedEvent message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[3] Cancel customer notify for OrderId={OrderId}", message.OrderId);
        return Task.CompletedTask;
    }
}

[ApiController]
[Route("api/eventbus")]
public sealed class EventBusController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public EventBusController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishOrderRequest request)
    {
        var message = new OrderCreatedEvent
        {
            Amount = request.Amount,
            Customer = request.Customer
        };

        await _publisher.AutoPublishAsync(message);
        return Results.Ok(message);
    }
}

public sealed class PublishOrderRequest
{
    public decimal Amount { get; set; } = 88.8m;

    public string Customer { get; set; } = "demo-customer";
}
