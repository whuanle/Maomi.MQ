using Maomi.MQ.EventBus;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Maomi.MQ.Tests.TypeFilter;

public partial class EventbusTypeFilterTests
{
    [Fact]
    public void No_EventTopic_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(NontTopicEventHandler)); });
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
    public void Usable_Attribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Usable_1_EventHandler));
        typeFilter.Filter(services, typeof(Usable_2_EventHandler));
        typeFilter.Build(services);

        var serviceDescriptors = services.ToArray();
        var handlerMediator = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IHandlerMediator<UsableEvent>));
        Assert.NotNull(handlerMediator);
        Assert.Equal(ServiceLifetime.Scoped, handlerMediator.Lifetime);

        var eventMiddleware = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IEventMiddleware<UsableEvent>));
        Assert.NotNull(eventMiddleware);
        Assert.Equal(ServiceLifetime.Scoped, eventMiddleware.Lifetime);
        Assert.Equal(typeof(DefaultEventMiddleware<UsableEvent>), eventMiddleware.ImplementationType);

        var eventBusConsumer = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<UsableEvent>) && object.Equals(x.ServiceKey, "test"));
        Assert.NotNull(eventBusConsumer);
        Assert.Equal(ServiceLifetime.Scoped, eventBusConsumer.Lifetime);

        var consumerOptions = serviceDescriptors.FirstOrDefault(x => object.Equals(x.ServiceKey, "test") && x.ServiceType == typeof(IConsumerOptions));
        Assert.NotNull(consumerOptions);
        Assert.Equal(ServiceLifetime.Singleton, consumerOptions.Lifetime);

        var eventInfo = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IEventHandlerFactory<UsableEvent>));
        Assert.NotNull(eventInfo);
        Assert.Equal(ServiceLifetime.Singleton, eventInfo.Lifetime);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);
        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
        Assert.Equal(typeof(ConsumerBaseHostService), consumerHostService.ImplementationFactory!.GetMethodInfo().ReturnType);
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
