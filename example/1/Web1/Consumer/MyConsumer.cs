using Maomi.MQ;
using Web1.Models;

namespace Web1.Consumer
{
    [Consumer("web1", Qos = 1)]
    public class MyConsumer : IConsumer<TestEvent>
    {

        // 消费
        public async Task ExecuteAsync(EventBody<TestEvent> message)
        {
            Console.WriteLine(message.Body.Message);
            throw new Exception("111");
        }

        // 每次失败时被执行
        public async Task FaildAsync(EventBody<TestEvent> message)
        {
            await Task.CompletedTask;
        }

        // 最后一次失败时执行
        public async Task FallbackAsync(EventBody<TestEvent> message)
        {

        }
    }

}
