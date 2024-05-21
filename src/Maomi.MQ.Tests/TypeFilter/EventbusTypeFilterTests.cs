using Maomi.MQ.Defaults;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Maomi.MQ.Tests.TypeFilter.ConsumerTypeFilterTests;

namespace Maomi.MQ.Tests.TypeFilter;

public class EventbusTypeFilterTests
{
    [Fact]
    public void No_EventTopicAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(Test1_0EventHandler)); });
    }

    [Fact]
    public void No_EventOrderAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(Test1_1EventHandler)); });
    }

    [Fact]
    public void Have_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Test2EventHandler));
        typeFilter.Build(services);

        var serviceDescriptors = services.ToArray();
        var handlerMediator = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(HandlerMediator<Test2Event>)); ;
        Assert.NotNull(handlerMediator);
        Assert.Equal(ServiceLifetime.Transient, handlerMediator.Lifetime);

        var consumerOptions = serviceDescriptors.FirstOrDefault(x => object.Equals(x.ServiceKey, typeof(Test2Event)) && x.ServiceType == typeof(ConsumerOptions));
        Assert.NotNull(consumerOptions);
        Assert.Equal(ServiceLifetime.Singleton, consumerOptions.Lifetime);

        var eventInfo = serviceDescriptors.FirstOrDefault(x => object.Equals(x.ServiceKey, typeof(Test2Event)) && x.ServiceType == typeof(EventInfo));
        Assert.NotNull(eventInfo);
        Assert.Equal(ServiceLifetime.Singleton, eventInfo.Lifetime);

        Assert.Equal(ServiceLifetime.Singleton, eventInfo.Lifetime);
        var eventMiddleware = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IEventMiddleware<Test2Event>)); ;
        Assert.NotNull(eventMiddleware);
        Assert.Equal(ServiceLifetime.Transient, eventMiddleware.Lifetime);

        var eventBusConsumer = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<Test2Event>)); ;
        Assert.NotNull(eventBusConsumer);
        Assert.Equal(ServiceLifetime.Transient, eventBusConsumer.Lifetime);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);
        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
        Assert.Equal(typeof(EventBusConsumerHostSrvice<EventBusConsumer<Test2Event>, Test2Event>), consumerHostService.ImplementationType);
    }

    // 事件分组

    public class Test1_0Event
    {
        public int Id { get; set; }
    }

    public class Test1_0EventHandler : IEventHandler<Test1_0Event>
    {
        public Task CancelAsync(EventBody<Test1_0Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task HandlerAsync(EventBody<Test1_0Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    public class Test1_1Event
    {
        public int Id { get; set; }
    }
    public class Test1_1EventHandler : IEventHandler<Test1_1Event>
    {
        public Task CancelAsync(EventBody<Test1_1Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task HandlerAsync(EventBody<Test1_1Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventTopic("test")]
    public class Test2Event
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class Test2EventHandler : IEventHandler<Test2Event>
    {
        public Task CancelAsync(EventBody<Test2Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task HandlerAsync(EventBody<Test2Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
