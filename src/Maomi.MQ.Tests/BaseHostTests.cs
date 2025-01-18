using Maomi.MQ.Tests.CustomConsumer;

namespace Maomi.MQ.Tests;

public class BaseHostTests : BaseMock
{
    public class ExceptionJsonSerializer : IMessageSerializer
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
    public class UnSetConsumer<TMessage> : IConsumer<TMessage>, IRetry, IEventBody<TMessage>
    where TMessage : class
    {
        public EventBody<TMessage> EventBody { get; private set; } = default!;

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TMessage> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TMessage>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TMessage>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }
}
