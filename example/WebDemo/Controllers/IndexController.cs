using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using WebDemo.MQ;

namespace WebDemo.Controllers;

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
        // 发布消息
        await _messagePublisher.PublishAsync(exchange: string.Empty, routingKey: "test1", message: new TestEvent
        {
            Id = 123
        });
        return "ok";
    }
}