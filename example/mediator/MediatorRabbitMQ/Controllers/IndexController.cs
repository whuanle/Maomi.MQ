using Maomi.MQ;
using Maomi.MQ.MediatR;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MediatorRabbitMQ.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(IMediator mediator, IMessagePublisher messagePublisher)
    {
        _mediator = mediator;
        _messagePublisher = messagePublisher;
    }

    [HttpGet]
    public async Task<string> Send()
    {
        await _mediator.Send(new MediatrMQCommand<MyCommand>
        {
            Message = new MyCommand
            {
                Name = "abcd"
            }
        });

        await _messagePublisher.PublishAsync(model: new MyCommand
        {
            Name = "abcd"
        });

        return "ok";
    }
}

[MediarCommand("mediator_consumer1", Qos = 1)]
public class MyCommand : IRequest
{
    public string Name { get; set; } = string.Empty;
}

public class MyCommand1Handler : IRequestHandler<MyCommand>
{
    public Task Handle(MyCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"MyCommand1Handler: {request.Name}");
        return Task.CompletedTask;
    }
}