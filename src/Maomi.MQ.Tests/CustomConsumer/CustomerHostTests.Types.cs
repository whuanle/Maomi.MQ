#pragma warning disable CS8618

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
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


    private class AllOptionsConsumerHostService : ConsumerHostService<AllOptionsConsumer, IdEvent>
    {
        public AllOptionsConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }

    #region WaitReady

    public abstract class WaitReadyConsumerHostService<TConsumer, TEvent> : ConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public DateTime InitTime { get; protected set; }
        public WaitReadyConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }
        protected override async Task WaitReadyAsync()
        {
            InitTime = DateTime.Now;
            await Task.Delay(1000);
            await base.WaitReadyAsync();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }

    public class WaitReady_0_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_0_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }
    }

    public class WaitReady_1_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_1_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }
    }
    public class WaitReady_2_ConsumerHostService<TConsumer, TEvent> : WaitReadyConsumerHostService<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
    {
        public WaitReady_2_ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }
    }

    private class TestDefaultConsumerHostService<TConsumer, TEvent> : ConsumerHostService<TConsumer, TEvent>
        where TEvent : class
        where TConsumer : IConsumer<TEvent>
    {
        public TestDefaultConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger) : base(serviceProvider, serviceFactory, logger)
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
        // Analog received data.
        public async Task PublishAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
        {
            var consumerOptions = _serviceProvider.GetRequiredKeyedService<IConsumerOptions>(serviceKey: GetConsumerType()[0].Queue);
            var consumer = new MessageConsumer(_serviceProvider,_serviceFactory,_serviceProvider.GetRequiredService<ILogger<MessageConsumer>>(), consumerOptions);
            await consumer.ConsumerAsync<TEvent>(channel, eventArgs);
        }
    }

    #endregion

    #region ConsumerOptions

    [Consumer("test",
        DeadQueue = "test_dead",
        ExecptionRequeue = true,
        Expiration = 1000,
        Group = "group",
        Qos = 10,
        RetryFaildRequeue = true)]
    private class AllOptionsConsumer : IConsumer<IdEvent>
    {
        public Task ExecuteAsync(EventBody<IdEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(Exception ex, int retryCount, EventBody<IdEvent>? message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FallbackAsync(EventBody<IdEvent>? message)
        {
            throw new NotImplementedException();
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
