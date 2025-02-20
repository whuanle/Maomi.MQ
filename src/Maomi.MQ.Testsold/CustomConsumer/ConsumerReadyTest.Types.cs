using Maomi.MQ.EventBus;
using System.Threading.Channels;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class ConsumerReadyTest
{
    [EventTopic("test_e")]
    private class TestEvent { }

    [Consumer("test")]
    private class MyConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
    }

    [Consumer("test2")]
    private class MyConsumer2 : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
    }

    [EventOrder(0)]
    public class TestEventHandler : IEventHandler<TestEvent>
    {
        Task IEventHandler<TestEvent>.CancelAsync(EventBody<TestEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;

        Task IEventHandler<TestEvent>.ExecuteAsync(EventBody<TestEvent> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
