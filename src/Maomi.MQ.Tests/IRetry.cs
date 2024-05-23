using static Maomi.MQ.Tests.CustomConsumer.DefaultCustomerHostTests;

namespace Maomi.MQ.Tests.CustomConsumer;

public interface IRetry
{
    public int RetryCount { get; }
    public bool IsFallbacked { get; }
}

public interface IEventBody<TEvent>
{
    public EventBody<TEvent> EventBody { get; }
}
