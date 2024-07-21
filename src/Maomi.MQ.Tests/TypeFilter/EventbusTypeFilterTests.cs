using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maomi.MQ.Tests.TypeFilter;

public partial class EventbusTypeFilterTests
{
    [Fact]
    public void No_EventTopic_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();

        typeFilter.Filter(services, typeof(NoneTopicEvent));
        Assert.Empty(services);

        // NontTopicEventHandler => NoneTopicEvent
        typeFilter.Filter(services, typeof(NontTopicEventHandler));
        Assert.Empty(services);

        typeFilter.Build(services);
        Assert.Empty(services);
    }

    [Fact]
    public void No_EventOrder_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(NoneOrderEventHandler)); });
    }

    [Fact]
    public void Equal_EventOrder_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(EqualOrderEvent));
        typeFilter.Filter(services, typeof(EqualOrder_1_EventHandler));
        Assert.Throws<ArgumentException>(() => { typeFilter.Filter(services, typeof(EqualOrder_2_EventHandler)); });
    }

    [Fact]
    public void Filter()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Usable_1_EventHandler));
        Assert.Single(services);

        // service 1.
        var serviceDescriptor = services.First();
        Assert.Equal(typeof(Usable_1_EventHandler), serviceDescriptor.ImplementationType);

        typeFilter.Build(services);
        Assert.Equal(7, services.Count);

        // service 2.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEventHandlerFactory<UsableEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        Assert.Equal("EventHandlerFactory`1", serviceDescriptor.ImplementationInstance!.GetType().Name);

        // service 3.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEventMiddleware< UsableEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        Assert.Equal("DefaultEventMiddleware`1", serviceDescriptor.ImplementationType!.Name);

        // service 4.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHandlerMediator<UsableEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        Assert.Equal("HandlerMediator`1", serviceDescriptor.ImplementationType!.Name);

        // service 5.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IConsumerOptions));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        Assert.Equal("test", serviceDescriptor.ServiceKey);
        Assert.Equal(typeof(EventTopicAttribute), serviceDescriptor.KeyedImplementationInstance!.GetType());

        // service 6.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<UsableEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        Assert.Equal("test", serviceDescriptor.ServiceKey);
        Assert.Equal(typeof(EventBusConsumer<UsableEvent>), serviceDescriptor.KeyedImplementationType);

        // service 7.
        serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void Filter_Middleware()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(MiddlewareEventHandler));
        Assert.Single(services);

        typeFilter.Filter(services, typeof(TestEventMiddleware));
        typeFilter.Build(services);

        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IEventMiddleware<MiddlewareEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
        Assert.Equal(typeof(TestEventMiddleware), serviceDescriptor.ImplementationType);
    }


    [Fact]
    public void Custom_Middleware()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(TestEventMiddleware));
        typeFilter.Filter(services, typeof(MiddlewareEventHandler));
        typeFilter.Build(services);

        var serviceDescriptors = services.ToArray();
        var eventMiddleware = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IEventMiddleware<MiddlewareEvent>));
        Assert.NotNull(eventMiddleware);
        Assert.Equal(ServiceLifetime.Scoped, eventMiddleware.Lifetime);
        Assert.Equal(typeof(TestEventMiddleware), eventMiddleware.ImplementationType);
    }
}
