using Maomi.MQ;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Web1.Models;

namespace Web1.Controllers
{
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
            for (var i = 0; i < 100; i++)
            {
                await _messagePublisher.PublishAsync(queue: "web1", message: new TestEvent
                {
                    Id = i
                });
            }

            return "ok";
        }
    }
}
