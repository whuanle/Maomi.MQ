using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Maomi.MQ.Tests.TypeFilter.EventbusTypeFilterTests;

namespace Maomi.MQ.Tests.TypeFilter;
public class EventbusGroupTypeFilterTests
{
    [Fact]
    public void EventGroup()
    {
        var services = new ServiceCollection();
        var typeFilter = new EventBusTypeFilter();
        typeFilter.Filter(services, typeof(Group_1_EventHandler));
        typeFilter.Filter(services, typeof(Group_2_EventHandler));
        typeFilter.Build(services);

        var serviceDescriptors = services.ToArray();

        var consumer1 = serviceDescriptors.FirstOrDefault(x => object.Equals(x.ServiceKey, "test1") && x.ServiceType == typeof(IConsumer<Group_1_Event>));
        var consumer2 = serviceDescriptors.FirstOrDefault(x => object.Equals(x.ServiceKey, "test2") && x.ServiceType == typeof(IConsumer<Group_2_Event>));

        Assert.NotNull(consumer1);
        Assert.NotNull(consumer2);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);
    }

    [EventTopic("test1", Group = "group", Qos = 10, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Group_1_Event
    {
        public int Id { get; set; }
    }

    [EventTopic("test2", Group = "group", Qos = 10, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Group_2_Event
    {
        public int Id { get; set; }
    }

    [EventOrder(0)]
    public class Group_1_EventHandler : IEventHandler<Group_1_Event>
    {
        public Task CancelAsync(EventBody<Group_1_Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<Group_1_Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [EventOrder(0)]
    public class Group_2_EventHandler : IEventHandler<Group_2_Event>
    {
        public Task CancelAsync(EventBody<Group_2_Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ExecuteAsync(EventBody<Group_2_Event> eventBody, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
