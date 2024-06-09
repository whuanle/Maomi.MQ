namespace Maomi.MQ.Tests.CustomConsumer;

public interface IEventBody<TEvent>
{
    public EventBody<TEvent> EventBody { get; }
}
