using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Maomi.MQ.Retry;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maomi.MQ.Tests.CustomConsumer;
public partial class EventGroupConsumerHostTests
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

    [EventTopic("test1", Group = "group")]
    public class Group_Test1Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test2", Group = "group")]
    public class Group_Test2Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test3", Group = "group", Qos = 10, RetryFaildRequeue = false, ExecptionRequeue = false)]
    public class FalseRequeueEvent_Group
    {
        public int Id { get; set; }
    }

    [EventTopic("test4", Group = "group", Qos = 10, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class TrueRequeueEvent_Group
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class TEventEventHandler<TEvent> : IEventHandler<TEvent>
    {
        public Task CancelAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task HandlerAsync(EventBody<TEvent> @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class EmptyConsumer<TEvent> : EventBusConsumer<TEvent>,IRetry,IEventBody<TEvent>
        where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; } = null!;
        public EmptyConsumer(
            IEventMiddleware<TEvent> eventMiddleware, 
            HandlerMediator<TEvent> handlerBroker, 
            ILogger<EventBusConsumer<TEvent>> logger, 
            IServiceProvider serviceProvider) : base(eventMiddleware, handlerBroker, logger, serviceProvider)
        {
        }

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public override Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }

        public override Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public override Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }

    public class ConsumerException<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
        where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }
        public EventBody<TEvent> EventBody { get; private set; } = null!;

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

    public class Retry_Faild_FallBack_False_Consumer<TEvent> : IConsumer<TEvent>, IRetry
        where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = false;
            return Task.FromResult(false);
        }
    }

    public class Retry_Faild_Fallback_True_Consumer<TEvent> : IConsumer<TEvent>, IRetry
                where TEvent : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
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
    public class TestDefaultConsumerHostService : EventGroupConsumerHostService
    {
        public TestDefaultConsumerHostService(
            IServiceProvider serviceProvider,
            DefaultMqOptions connectionOptions,
            IJsonSerializer jsonSerializer,
            ILogger<EventGroupConsumerHostService> logger,
            IRetryPolicyFactory policyFactory,
            IWaitReadyFactory waitReadyFactory,
            EventGroupInfo eventGroupInfo) : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, waitReadyFactory, eventGroupInfo)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public async Task PublishAsync<TEvent>(EventBus.EventInfo eventInfo, IChannel channel, BasicDeliverEventArgs eventArgs)
            where TEvent : class
        {
            await ConsumerAsync<TEvent>(eventInfo, channel, eventArgs);
        }
    }
}
