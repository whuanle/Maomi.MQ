using FastEndpoints;
using Maomi.MQ;

namespace FastEndpointsDemo.Controllers;

public class CreateOrderEndpoint : Endpoint<SendMQ, string>
{
    private readonly IMessagePublisher _messagePublisher;

    public CreateOrderEndpoint(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public override void Configure()
    {
        Post("/send");
        AllowAnonymous();
    }
    public override async Task<string> ExecuteAsync(SendMQ req, CancellationToken ct)
    {
        // Send event message, 1
        await PublishAsync(new OrderCreatedEvent
        {
            OrderID = "001",
            CustomerName = req.Name,
            OrderTotal = 100
        });

        // Send event message, 2
        await _messagePublisher.AutoPublishAsync(message: new OrderCreatedEvent
        {
            OrderID = "001",
            CustomerName = req.Name,
            OrderTotal = 100
        });

        // Send command message, 1
        await new OrderCreatedCommand()
        {
            OrderID = "001",
            CustomerName = req.Name,
            OrderTotal = 100
        }
        .ExecuteAsync();

        await _messagePublisher.AutoPublishAsync(message: new OrderCreatedCommand
        {
            OrderID = "001",
            CustomerName = req.Name,
            OrderTotal = 100
        });

        // Send command message, 2

        return "ok";
    }

    [FCommand("fastendpoints_consumer1", Qos = 1)]
    public class OrderCreatedEvent : IEvent
    {
        public string OrderID { get; set; }
        public string CustomerName { get; set; }
        public decimal OrderTotal { get; set; }
    }

    public class OrderCreationHandler : IEventHandler<OrderCreatedEvent>
    {
        private readonly ILogger _logger;

        public OrderCreationHandler(ILogger<OrderCreationHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(OrderCreatedEvent eventModel, CancellationToken ct)
        {
            _logger.LogInformation($"order created event received:[{eventModel.OrderID}]");
            return Task.CompletedTask;
        }
    }

    [FCommand("fastendpoints_consumer2", Qos = 1)]
    public class OrderCreatedCommand : ICommand
    {
        public string OrderID { get; set; }
        public string CustomerName { get; set; }
        public decimal OrderTotal { get; set; }
    }

    public class OrderCreatedCommandHandler : ICommandHandler<OrderCreatedCommand>
    {
        private readonly ILogger _logger;

        public OrderCreatedCommandHandler(ILogger<OrderCreationHandler> logger)
        {
            _logger = logger;
        }

        public Task ExecuteAsync(OrderCreatedCommand command, CancellationToken ct)
        {
            _logger.LogInformation($"order created event received:[{command.OrderID}]");
            return Task.CompletedTask;
        }
    }
}