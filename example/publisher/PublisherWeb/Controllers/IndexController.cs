using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using PublisherWeb.MQ;
using RabbitMQ.Client;

namespace PublisherWeb.Controllers;

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
        // 普通发布模式
        for (var i = 0; i < 100; i++)
        {
            await _messagePublisher.PublishAsync(exchange: string.Empty, routingKey: "publish", message: new TestEvent
            {
                Id = i
            });
        }

        return "ok";
    }

    [HttpGet("publish_message")]
    public async Task<string> PublisherMessage()
    {
        // 如果在本项目中 TestMessageEvent 只指定了一个消费者，那么通过 TestMessageEvent 自动寻找对应的配置
        for (var i = 0; i < 100; i++)
        {
            await _messagePublisher.AutoPublishAsync(message: new TestMessageEvent
            {
                Id = i
            });
        }

        return "ok";
    }

    [HttpGet("publish_tran")]
    public async Task<string> Publisher_Tran()
    {
        using var tranPublisher = _messagePublisher.CreateTransaction();
        await tranPublisher.TxSelectAsync();

        try
        {
            await tranPublisher.PublishAsync(exchange: string.Empty, routingKey: "publish", message: new TestEvent
            {
                Id = 666
            });
            await Task.Delay(5000);
            await tranPublisher.TxCommitAsync();
        }
        catch
        {
            await tranPublisher.TxRollbackAsync();
            throw;
        }

        return "ok";
    }

    [HttpGet("publish_confirm")]
    public async Task<string> Publisher_Confirm()
    {
        using var confirmPublisher = _messagePublisher.CreateSingle(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true));

        for (var i = 0; i < 5; i++)
        {
            await confirmPublisher.PublishAsync(exchange: string.Empty, routingKey: "publish", message: new TestEvent
            {
                Id = 666
            });
        }

        return "ok";
    }


    [HttpGet("publish_fanout")]
    public async Task<string> Publisher_Fanout()
    {
        for (var i = 0; i < 5; i++)
        {
            await _messagePublisher.PublishAsync(exchange: "fanouttest", routingKey: string.Empty, message: new FanoutEvent
            {
                Id = 666
            });
        }

        return "ok";
    }

    [HttpGet("publish_topic")]
    public async Task<string> Publisher_Topic()
    {
        for (var i = 0; i < 5; i++)
        {
            await _messagePublisher.PublishAsync(exchange: "topictest", routingKey: "red.a", message: new TopicEvent
            {
                Id = 666
            });
            await _messagePublisher.PublishAsync(exchange: "topictest", routingKey: "red.yellow.a", message: new TopicEvent
            {
                Id = 666
            });
        }

        return "ok";
    }
}
