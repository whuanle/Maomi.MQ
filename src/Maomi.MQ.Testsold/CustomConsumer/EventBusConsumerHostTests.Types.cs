using Maomi.MQ.Default;
using Maomi.MQ.EventBus;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class EventBusConsumerHostTests
{
    [EventTopic("test")]
    private class IdEvent
    {
        public int Id { get; set; }
    }

    [EventTopic("test",
        DeadRoutingKey = "test_dead",
        ExecptionRequeue = true,
        Expiration = 1000,
        Qos = 10,
        RetryFaildRequeue = true)]
    private class AllOptionsEvent
    {
    }


    [EventTopic("test1")]
    private class WaitReady_1_Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test2")]
    private class WaitReady_2_Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test3")]
    private class WaitReady_3_Event
    {
        public int Id { get; set; }
    }

    #region WaitReady

    private abstract class WaitReadyConsumerHostService: EventBusHostService
    {
        protected WaitReadyConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }

        public DateTime InitTime { get; protected set; }


        protected override async Task WaitReadyInitQueueAsync(IConnection connection)
        {
            InitTime = DateTime.Now;
            await Task.Delay(1000);
            await base.WaitReadyInitQueueAsync(connection);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return base.ExecuteAsync(stoppingToken);
        }
    }

    private class WaitReady_0_ConsumerHostService : WaitReadyConsumerHostService
    {
        public WaitReady_0_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }
    }

    private class WaitReady_1_ConsumerHostService : WaitReadyConsumerHostService
    {
        public WaitReady_1_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }
    }

    private class WaitReady_2_ConsumerHostService : WaitReadyConsumerHostService
    {
        public WaitReady_2_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }
    }

    private class TestDefaultConsumerHostService : WaitReadyConsumerHostService
    {
        public TestDefaultConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return base.ExecuteAsync(stoppingToken);
        }

        // Analog received data.
        public async Task PublishAsync<TMessage>(string queue,IChannel channel, BasicDeliverEventArgs eventArgs)
            where TMessage:class
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: queue);
            var consumer = new MessageConsumer(_serviceProvider, _serviceFactory, _serviceProvider.GetRequiredService<ILogger<MessageConsumer>>(), consumerOptions);
            await consumer.ConsumerAsync<TMessage>(channel, eventArgs);
        }
    }
    #endregion

    private class WaitReadyTMessageEventHandler<TMessage> : IEventHandler<TMessage>
    {
        public Task CancelAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [EventOrder(0)]
    private class WaitReady_1_EventHandler : WaitReadyTMessageEventHandler<WaitReady_1_Event>
    {
    }

    [EventOrder(0)]
    private class WaitReady_2_EventHandler : WaitReadyTMessageEventHandler<WaitReady_2_Event>
    {
    }

    [EventOrder(0)]
    private class WaitReady_3_EventHandler : WaitReadyTMessageEventHandler<WaitReady_3_Event>
    {
    }

    private class AllOptionsConsumerHostService : EventBusHostService
    {
        public AllOptionsConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerHostedService> logger, IReadOnlyList<ConsumerType> consumerTypes) : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
        {
        }
    }

    [EventTopic("test", Qos = 10, RetryFaildRequeue = false, ExecptionRequeue = false)]
    public class FalseRequeueEvent_Group
    {
        public int Id { get; set; }
    }

    [EventTopic("test", Qos = 10, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class TrueRequeueEvent_Group
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class TMessageEventHandler<TMessage> : IEventHandler<TMessage>
    {
        public Task CancelAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [EventOrder(0)]
    private class ExceptionEventHandler<TMessage> : IEventHandler<TMessage>
    {
        public Task CancelAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EventBody<TMessage> message, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }

    public class EmptyConsumer<TMessage> : EventBusConsumer<TMessage>, IRetry, IEventBody<TMessage>
        where TMessage : class
    {
        public EmptyConsumer(IEventMiddleware<TMessage> eventMiddleware, IHandlerMediator<TMessage> handlerBroker, ILogger<EventBusConsumer<TMessage>> logger) : base(eventMiddleware, handlerBroker, logger)
        {
        }

        public EventBody<TMessage> EventBody { get; private set; } = null!;

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public override Task ExecuteAsync(EventBody<TMessage> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }

        public override Task FaildAsync(Exception ex, int retryCount, EventBody<TMessage>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public override Task<bool> FallbackAsync(EventBody<TMessage>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }

    public class ConsumerException<TMessage> : IConsumer<TMessage>, IRetry, IEventBody<TMessage>
        where TMessage : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }
        public EventBody<TMessage> EventBody { get; private set; } = null!;

        public Task ExecuteAsync(EventBody<TMessage> message)
        {
            EventBody = message;
            throw new OperationCanceledException();
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


    public class Retry_Faild_FallBack_False_Consumer<TMessage> : EventBusConsumer<TMessage>, IRetry
        where TMessage : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }
        public Retry_Faild_FallBack_False_Consumer(IEventMiddleware<TMessage> eventMiddleware, IHandlerMediator<TMessage> handlerBroker, ILogger<EventBusConsumer<TMessage>> logger) : base(eventMiddleware, handlerBroker, logger)
        {
        }

        public override Task ExecuteAsync(EventBody<TMessage> message)
        {
            return base.ExecuteAsync(message);
        }
        public override Task FaildAsync(Exception ex, int retryCount, EventBody<TMessage>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public override Task<bool> FallbackAsync(EventBody<TMessage>? message)
        {
            IsFallbacked = false;
            return Task.FromResult(false);
        }
    }

    public class Retry_Faild_Fallback_True_Consumer<TMessage> : IConsumer<TMessage>, IRetry
                where TMessage : class
    {
        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TMessage> message)
        {
            throw new OperationCanceledException();
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TMessage>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TMessage>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(true);
        }
    }
}
