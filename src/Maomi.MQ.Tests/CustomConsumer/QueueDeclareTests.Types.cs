namespace Maomi.MQ.Tests.CustomConsumer;

public partial class QueueDeclareTests
{
    public class TestEvent { }

    [Consumer("test",
        DeadRoutingKey = "test_dead",
        ExecptionRequeue = true,
        Expiration = 1000,
        Qos = 10,
        RetryFaildRequeue = true)]
    private class AllOptionsConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;

        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;

        public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
    }
}
