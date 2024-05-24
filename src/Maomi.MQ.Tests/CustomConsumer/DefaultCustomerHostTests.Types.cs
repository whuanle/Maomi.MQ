using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Tests.CustomConsumer;
public partial class DefaultCustomerHostTests
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

    public class IdEvent
    {
        public int Id { get; set; }
    }

    [Consumer("test")]
    public class EmptyConsumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
        where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; }

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

    [Consumer("test", Qos = 1, RetryFaildRequeue = false, ExecptionRequeue = false)]
    public class Exception_NoRequeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
        where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; }

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            throw new OperationCanceledException();
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

    [Consumer("test", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Exception_Requeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
    where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; }

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            throw new OperationCanceledException();
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

    [Consumer("test", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Retry_Faild_Fallback_False_Requeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
        where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }
        public EventBody<TEvent> EventBody { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            throw new OperationCanceledException();
        }
    }

    [Consumer("test", Qos = 1)]
    public class Retry_Faild_Fallback_True_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
    where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }
        public EventBody<TEvent> EventBody { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(true);
        }
    }

    public class TestDefaultConsumerHostService<TConsumer, TEvent> : DefaultConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public TestDefaultConsumerHostService(IServiceProvider serviceProvider, DefaultMqOptions connectionOptions, IJsonSerializer jsonSerializer, ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger, IRetryPolicyFactory policyFactory, IWaitReadyFactory waitReadyFactory) : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        // Analog received data.
        public async Task PublishAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
        {
            await base.ConsumerAsync(channel, eventArgs);
        }
    }
}
