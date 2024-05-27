using ActivitySourceApi.Models;
using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;

namespace ActivitySourceApi.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        for (var i = 0; i < 1; i++)
        {
            await _messagePublisher.PublishAsync(queue: "ActivitySourceApi", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }
}