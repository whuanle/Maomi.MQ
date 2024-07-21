#pragma warning disable CS8618

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Tests.CustomConsumer;
public partial class DefaultCustomerHostTests
{
    private class IdEvent
    {
        public int Id { get; set; }
    }

    #region WaitReady

    public abstract class WaitReadyConsumerHostService : ConsumerBaseHostService
    {
        public DateTime InitTime { get; protected set; }
        public WaitReadyConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }

        protected override async Task WaitReadyInitQueueAsync(IConnection connection)
        {
            InitTime = DateTime.Now;
            await Task.Delay(1000);
            await base.WaitReadyInitQueueAsync(connection);
        }
    }

    public abstract class WaitReadyConsumerHostService<TConsumer, TEvent> : ConsumerBaseHostService
    {
        public DateTime InitTime { get; protected set; }
        public WaitReadyConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }

        protected override async Task WaitReadyInitQueueAsync(IConnection connection)
        {
            InitTime = DateTime.Now;
            await Task.Delay(1000);
            await base.WaitReadyInitQueueAsync(connection);
        }
    }

    public class WaitReady_0_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_0_ConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }
    }

    public class WaitReady_1_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_1_ConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }
    }
    public class WaitReady_2_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_2_ConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }
    }

    private class TestDefaultConsumerHostService : WaitReadyConsumerHostService
    {
        public TestDefaultConsumerHostService(
            IServiceProvider serviceProvider,
            ServiceFactory serviceFactory,
            ConnectionPool connectionPool,
            ILogger<ConsumerBaseHostService> logger,
            List<ConsumerType> consumers)
            : base(serviceProvider, serviceFactory, connectionPool, logger, consumers)
        {
        }
        // Analog received data.
        public async Task PublishAsync<TEvent>(string queue, IChannel channel, BasicDeliverEventArgs eventArgs)
            where TEvent : class
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: queue);
            var consumer = new MessageConsumer(_serviceProvider, _serviceFactory, _serviceProvider.GetRequiredService<ILogger<MessageConsumer>>(), consumerOptions);
            await consumer.ConsumerAsync<TEvent>(channel, eventArgs);
        }
    }

    #endregion

    [Consumer("test", Qos = 1, RetryFaildRequeue = false, ExecptionRequeue = false)]
    private class Exception_NoRequeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
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
    private class Exception_Requeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
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
    private class Retry_Faild_Fallback_False_Requeue_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
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
    private class Retry_Faild_Fallback_True_Consumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
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
}
