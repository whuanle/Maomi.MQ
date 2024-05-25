using Maomi.MQ;
using Maomi.MQ.EventBus;

namespace Web3.Events
{
    [EventTopic("web3_1",Group = "aaa")]
    public class Test1Event
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }

    [EventTopic("web3_2",Group = "aaa")]
    public class Test2Event
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }

    [EventOrder(0)]
    public class My1EventEventHandler : IEventHandler<Test1Event>
    {
        public async Task CancelAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(1)]
    public class My2EventEventHandler : IEventHandler<Test1Event>
    {
        public async Task CancelAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }
    }


    public class Test2EventMiddleware : IEventMiddleware<Test2Event>
    {
        public Task FaildAsync(Exception ex, int retryCount, EventBody<Test2Event>? message)
        {
            return Task.CompletedTask;
        }

        public Task<bool> FallbackAsync(EventBody<Test2Event>? message)
        {
            return Task.FromResult(true);
        }

        public async Task ExecuteAsync(EventBody<Test2Event> @event, EventHandlerDelegate<Test2Event> next)
        {
            await next(@event, CancellationToken.None);
        }
    }

    [EventOrder(0)]
    public class My1Event2EventHandler : IEventHandler<Test2Event>
    {
        public async Task CancelAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(1)]
    public class My2Event2EventHandler : IEventHandler<Test2Event>
    {
        public async Task CancelAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }
    }
}
