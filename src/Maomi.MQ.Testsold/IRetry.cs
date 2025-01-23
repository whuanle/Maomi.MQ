namespace Maomi.MQ.Tests.CustomConsumer;

public interface IRetry
{
    public int RetryCount { get; }
    public bool IsFallbacked { get; }
}
