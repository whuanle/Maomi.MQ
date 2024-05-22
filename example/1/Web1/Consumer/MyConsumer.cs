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
            Console.WriteLine(message.Body.Id);
            throw new Exception("111");
        }

        // 每次失败时被执行
        public async Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
        {
            Console.WriteLine($"重试 {message.Body.Id}");
            await Task.CompletedTask;
        }


        // 最后一次失败时执行
        public async Task<bool> FallbackAsync(EventBody<TestEvent>? message)
        {
            Console.WriteLine($"最后一次 {message.Body.Id}");
            return true;
        }
    }
}
