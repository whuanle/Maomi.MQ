using ActivitySourceApi.Models;
using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace ActivitySourceApi.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;
    static Meter s_meter = new("Microsoft.AspNetCore.Hosting1", "1.0.0");
    static Counter<int> s_hatsSold = s_meter.CreateCounter<int>("hats-sold");

    public IndexController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        for (var i = 0; i < 2; i++)
        {
            s_hatsSold.Add(1000);

            await _messagePublisher.PublishAsync(queue: "ActivitySourceApi", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }
}