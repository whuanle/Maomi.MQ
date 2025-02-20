using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using ConsumerWeb.Models;
using ConsumerWeb.Consumer;

namespace ConsumerWeb.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly IDynamicConsumer _dynamicConsumer;

    public IndexController(IMessagePublisher messagePublisher, IDynamicConsumer dynamicConsumer)
    {
        _messagePublisher = messagePublisher;
        _dynamicConsumer = dynamicConsumer;
    }

    [HttpPost("create")]
    public async Task<string> CreateConsumer([FromBody] ConsumerDto consumer)
    {
        foreach (var item in consumer.Queues)
        {
            var consumerTag = await _dynamicConsumer.ConsumerAsync<MyConsumer, TestEvent>(new ConsumerOptions(item));
        }

        return "ok";
    }

    [HttpPost("create")]
    public async Task<string> CreateConsumer([FromBody] ConsumerDto consumer)
    {
        foreach (var item in consumer.Queues)
        {
            var consumerTag = await _dynamicConsumer.ConsumerAsync<TestEvent>(
                consumerOptions: new ConsumerOptions(item),
                execute: async (header, message) =>
                {
                    await Task.CompletedTask;
                },
                faild: async (header, ex, retryCount, message) => { },
                fallback: async (header, message, ex) => ConsumerState.Ack
                );
        }

        return "ok";
    }

    [HttpPost("stop")]
    public async Task<string> StopConsumer([FromBody] ConsumerDto consumer)
    {
        foreach (var item in consumer.Queues)
        {
            await _dynamicConsumer.StopConsumerAsync(item);
        }

        return "ok";
    }

    [HttpPost("publish")]
    public async Task<string> Publisher([FromBody] ConsumerDto consumer)
    {
        List<Task> tasks = new();
        foreach (var item in consumer.Queues)
        {
            var task = Task.Run(async () =>
            {
                for (var i = 0; i < 10_0000; i++)
                {
                    await _messagePublisher.PublishAsync(queue: item, message: new TestEvent
                    {
                        Id = i,
                    });
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        return "ok";
    }
}

public class ConsumerDto
{
    /// <summary>
    /// 队列名称.
    /// </summary>
    public string[] Queues { get; set; } = null!;
}