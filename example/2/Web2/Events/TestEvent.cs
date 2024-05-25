using Maomi.MQ;
using Maomi.MQ.EventBus;

namespace Web2.Events
{
    [EventTopic("web2")]
    public class TestEvent
    {
        public string Message { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }


    public class TestEventMiddleware : IEventMiddleware<TestEvent>
    {
        public async Task ExecuteAsync(EventBody<TestEvent> @event, EventHandlerDelegate<TestEvent> next)
        {
            await next(@event, CancellationToken.None);
        }
    }

    [EventOrder(0)]
    public class My1EventEventHandler : IEventHandler<TestEvent>
    {
        public async Task CancelAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{@event.Id},事件 1 已被执行");
        }
    }

    [EventOrder(1)]
    public class My2EventEventHandler : IEventHandler<TestEvent>
    {
        public async Task CancelAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
        {
        }

        public async Task ExecuteAsync(EventBody<TestEvent> @event, CancellationToken cancellationToken)
        {
            Console.WriteLine($"{@event.Id},事件 2 已被执行");
        }
    }
}
