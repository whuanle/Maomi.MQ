using Maomi.MQ.Tests.CustomConsumer;

namespace Maomi.MQ.Tests;

public class BaseHostTests : BaseMock
{
    public class ExceptionJsonSerializer : IJsonSerializer
    {
        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes) where TObject : class
        {
            throw new NotImplementedException();
        }

        public byte[] Serializer<TObject>(TObject obj) where TObject : class
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }

    [Consumer("test")]
    public class UnSetConsumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
    where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; } = default!;

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }
}
