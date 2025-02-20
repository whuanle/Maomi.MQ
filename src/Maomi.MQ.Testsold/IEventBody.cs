namespace Maomi.MQ.Tests.CustomConsumer;

public interface IEventBody<TMessage>
{
    public EventBody<TMessage> EventBody { get; }
}
