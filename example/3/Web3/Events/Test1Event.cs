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


    public class TestEventMiddleware : IEventMiddleware<Test1Event>
    {
        public async Task HandleAsync(EventBody<Test1Event> @event, EventHandlerDelegate<Test1Event> next)
        {
            await next(@event, CancellationToken.None);
        }
    }

    [EventOrder(0)]
    public class My1EventEventHandler : IEventHandler<Test1Event>
    {
        public async Task CancelAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(1)]
    public class My2EventEventHandler : IEventHandler<Test1Event>
    {
        public async Task CancelAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<Test1Event> @event, CancellationToken cancellationToken)
        {
        }
    }


    public class Test2EventMiddleware : IEventMiddleware<Test2Event>
    {
        public async Task HandleAsync(EventBody<Test2Event> @event, EventHandlerDelegate<Test2Event> next)
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

        public async Task HandlerAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }
    }

    [EventOrder(1)]
    public class My2Event2EventHandler : IEventHandler<Test2Event>
    {
        public async Task CancelAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }

        public async Task HandlerAsync(EventBody<Test2Event> @event, CancellationToken cancellationToken)
        {
        }
    }
}
