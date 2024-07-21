using ConsumerWeb.Models;
using Maomi.MQ;

namespace ConsumerWeb.Consumer;
public interface IDynamicConsumer
{
    Task ExecuteAsync(EventBody<TestEvent> message);
    Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message);
    Task<bool> FallbackAsync(EventBody<TestEvent>? message);
}