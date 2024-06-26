using EventGroup.Events;
using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace EventGroup.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    private readonly ILogger<IndexController> _logger;

    public IndexController(ILogger<IndexController> logger, IMessagePublisher messagePublisher)
    {
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        for (var i = 0; i < 100; i++)
        {
            await _messagePublisher.PublishAsync("EventGroup_1", new Test1Event
            {
                Message = i.ToString()
            });
            await _messagePublisher.PublishAsync("EventGroup_2", new Test2Event
            {
                Message = i.ToString()
            });
        }

        return "ok";
    }
}
