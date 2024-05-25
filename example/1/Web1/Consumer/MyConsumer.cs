using Maomi.MQ;
using Web1.Models;

namespace Web1.Consumer
{
    [Consumer("web1", Qos = 1 , RetryFaildRequeue = true)]
    public class MyConsumer : IConsumer<TestEvent>
    {
        private  int _retryCount = 0;
        // 消费
        public async Task ExecuteAsync(EventBody<TestEvent> message)
        {
            Console.WriteLine($"执行 {message.Body.Id} 第几次：{_retryCount} {DateTime.Now}");
            _retryCount++;
            throw new Exception("1");
        }

        // 每次失败时被执行
        public async Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
        {
            Console.WriteLine($"重试 {message.Body.Id} 第几次：{retryCount} {DateTime.Now}");
            await Task.CompletedTask;
        }


        // 最后一次失败时执行
        public async Task<bool> FallbackAsync(EventBody<TestEvent>? message)
        {
            Console.WriteLine($"执行 {message.Body.Id} 补偿 {DateTime.Now}");
            return true;
        }
    }
}
