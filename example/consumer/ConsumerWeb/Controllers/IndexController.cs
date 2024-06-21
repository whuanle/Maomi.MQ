using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using ConsumerWeb.Models;

namespace ConsumerWeb.Controllers;

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
            await _messagePublisher.PublishAsync(queue: "ConsumerWeb", message: new TestEvent
            {
                Id = i
            }, properties =>
            {
                properties.Expiration = "6000";
            });
        }

        return "ok";
    }

    [HttpGet("publish_dead")]
    public async Task<string> PublisherDead()
    {
        for (var i = 0; i < 1; i++)
        {
            await _messagePublisher.PublishAsync(queue: "ConsumerWeb_dead", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }

    [HttpGet("publish_exchange")]
    public async Task<string> PublisherExchange()
    {
        var exchange = _messagePublisher.CreateExchange();
        for (var i = 0; i < 1; i++)
        {
            await exchange.PublishAsync(queue: "exchange", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }

    [HttpGet("publish_exchange1")]
    public async Task<string> PublisherExchange1()
    {
        var exchange = _messagePublisher.CreateExchange();
        using var single = exchange.CreateSingle();
        for (var i = 0; i < 1; i++)
        {
            await single.PublishAsync(queue: "exchange", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }
}