using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace QosPublisher.Controllers;

[ApiController]
[Route("[controller]")]
public class IndexController : ControllerBase
{
    private readonly IMessagePublisher _messagePublisher;

    public IndexController(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <summary>
    ///  100 w条数据
    /// </summary>
    /// <returns></returns>
    [HttpGet("publish")]
    public async Task<string> Publisher()
    {
        int totalCount = 0;
        List<Task> tasks = new();
        var message = string.Join(",", Enumerable.Range(0, 100));
        var data = Enumerable.Range(0, 100).ToArray();
        for (var i = 0; i < 10; i++)
        {
            var task = Task.Factory.StartNew(async () =>
            {
                var singlePublisher = _messagePublisher.CreateSingle();

                for (int k = 0; k < 100000; k++)
                {
                    var count = Interlocked.Increment(ref totalCount);
                    await singlePublisher.PublishAsync(queue: "qos", message: new TestEvent
                    {
                        Id = count,
                        Message = message,
                        Data = data
                    });
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return "ok";
    }
}

[Consumer("qos")]
public class QosConsumer : EmptyConsumer<TestEvent>, IEmptyConsumer<TestEvent> { }