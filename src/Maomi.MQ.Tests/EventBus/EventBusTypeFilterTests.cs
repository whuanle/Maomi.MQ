using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maomi.MQ.Tests;

public class EventBusTypeFilterTests
{
    [Fact]
    public void Build_ShouldReturnEmpty_WhenNoEventInfos()
    {
        var services = new ServiceCollection();
        var filter = new EventBusTypeFilter();

        var result = filter.Build(services);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ShouldReturnNull_WhenTypeNoEventTopicAttribute()
    {
        var services = new ServiceCollection();
        var filter = new EventBusTypeFilter();
        var type = typeof(EmptyTestEventHandler);

        filter.Filter(services, type);

        var result = filter.Build(services);
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ShouldAddScopedService_WhenTypeIsEventHandler()
    {
        var services = new ServiceCollection();
        var filter = new EventBusTypeFilter();
        var type = typeof(TestEventHandler);

        filter.Filter(services, type);

        var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == type);
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void Filter_ShouldNotAddService_WhenTypeIsNotEventHandlerOrMiddleware()
    {
        var services = new ServiceCollection();
        var filter = new EventBusTypeFilter();
        var type = typeof(NotEventHandlerOrMiddleware);

        filter.Filter(services, type);

        var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == type);
        Assert.Null(serviceDescriptor);
    }

    [Fact]
    public void Build_ShouldReturnConsumerType()
    {
        var services = new ServiceCollection();
        var filter = new EventBusTypeFilter();
        filter.Filter(services, typeof(TestEventHandler));
        var result = filter.Build(services).ToArray();

        Assert.Single(result);
        Assert.Equal(typeof(TestEvent), result[0].Event);

        var consumerOptions = result[0].ConsumerOptions;
        Assert.Equal("test", consumerOptions.Queue);
    }

    [Fact]
    public void Filter_ShouldAddService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var filter = new EventBusTypeFilter();

        filter.Filter(services, typeof(TestEventHandler));
        filter.Filter(services, typeof(TestEventMiddleware));

        _ = filter.Build(services);

         var serviceProvider =services.BuildServiceProvider();

        var eventMiddleware = serviceProvider.GetService<IEventMiddleware<TestEvent>>();
        Assert.NotNull(eventMiddleware);
        Assert.IsType<TestEventMiddleware>(eventMiddleware);

        var eventHandlerFactory = serviceProvider.GetService<IEventHandlerFactory<TestEvent>>();
        Assert.NotNull(eventHandlerFactory);
        Assert.IsType<EventHandlerFactory<TestEvent>>(eventHandlerFactory);

        var handlerMediator = serviceProvider.GetService<IHandlerMediator<TestEvent>>();
        Assert.NotNull(handlerMediator);
        Assert.IsType<HandlerMediator<TestEvent>>(handlerMediator);

        var consumer = serviceProvider.GetService<IConsumer<TestEvent>>();
        Assert.NotNull(consumer);
        Assert.IsType<EventBusConsumer<TestEvent>>(consumer);
    }

    [EventOrder(1)]
    private class TestEventHandler : IEventHandler<TestEvent>
    {
        public Task CancelAsync(TestEvent message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(TestEvent message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class TestEventMiddleware : IEventMiddleware<TestEvent>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message, EventHandlerDelegate<TestEvent> next)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent? message)
        {
            throw new NotImplementedException();
        }

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
        {
            throw new NotImplementedException();
        }
    }

    private class EmptyTestEventHandler : IEventHandler<EmptyTestEvent>
    {
        public Task CancelAsync(EmptyTestEvent message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(EmptyTestEvent message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class NotEventHandlerOrMiddleware { }

    [EventTopic("test")]
    private class TestEvent { }

    private class EmptyTestEvent { }
}
