using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Maomi.MQ.Tests.TypeFilter;
public class ConsumerTypeFilterTests
{
    [Fact]
    public void No_ConsumerAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();

        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(NoneConsumer)); });
        Assert.Throws<ArgumentNullException>(() => { ConsumerHostService<NoneConsumer, TestEvent>.GetConsumerType(); });
    }

    [Fact]
    public void GetGetConsumerType()
    {
        var consumerAttribute = typeof(UseConsumer).GetCustomAttribute<ConsumerAttribute>();
        Assert.NotNull(consumerAttribute);

        var consumerTypes = ConsumerHostService<UseConsumer, TestEvent>.GetConsumerType();
        Assert.Single(consumerTypes);

        var consumerType = consumerTypes[0];

        Assert.Equal(consumerAttribute.Queue, consumerType.Queue);
        Assert.Equal(typeof(UseConsumer), consumerType.Consumer);
        Assert.Equal(typeof(TestEvent), consumerType.Event);
    }

    [Fact]
    public void Have_ConsumerAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UseConsumer));

        var serviceDescriptors = services.ToArray();
        var consumerService = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<TestEvent>));
        Assert.NotNull(consumerService);
        Assert.Equal(ServiceLifetime.Scoped, consumerService.Lifetime);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);

        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
        Assert.Equal(typeof(ConsumerHostService<UseConsumer, TestEvent>), consumerHostService.ImplementationType);
    }

    [Fact]
    public void ConsumerAttributeInfo()
    {
        var consumerAttribute = typeof(UseConsumer).GetCustomAttribute<ConsumerAttribute>();
        Assert.NotNull(consumerAttribute);

        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UseConsumer));

        var ioc = services.BuildServiceProvider();
        var consumerOptions = ioc.GetRequiredKeyedService<IConsumerOptions>(serviceKey: "test");
        Assert.Equal(consumerAttribute, consumerOptions);
    }

    private class TestEvent
    {
        public int Id { get; set; }
    }

    private class NoneConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
    }

    [Consumer("test", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    private class UseConsumer : NoneConsumer, IConsumer<TestEvent>
    {
    }
}
