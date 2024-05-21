using Maomi.MQ.Defaults;
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

        Assert.Throws<ArgumentNullException>(() => { typeFilter.Filter(services, typeof(Consumer1)); });
        Assert.Throws<ArgumentNullException>(() => { DefaultConsumerHostService<Consumer1, TestEvent1>.GetConsumerOptions(); });
    }

    [Fact]
    public void Have_ConsumerAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer2));

        var serviceDescriptors = services.ToArray();
        var consumerService = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<TestEvent1>));
        Assert.NotNull(consumerService);
        Assert.Equal(ServiceLifetime.Transient, consumerService.Lifetime);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);

        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
        Assert.Equal(typeof(DefaultConsumerHostService<Consumer2, TestEvent1>), consumerHostService.ImplementationType);
    }

    [Fact]
    public void GetConsumerOptions()
    {
        var consumerAttribute = typeof(Consumer2).GetCustomAttribute<ConsumerAttribute>();
        Assert.NotNull(consumerAttribute);
        var consumerOptions = DefaultConsumerHostService<Consumer2, TestEvent1>.GetConsumerOptions();

        Assert.Equal(consumerAttribute.Queue,consumerOptions.Queue);
        Assert.Equal(consumerAttribute.Qos,consumerOptions.Qos);
        Assert.Equal(consumerAttribute.RetryFaildRequeue,consumerOptions.RetryFaildRequeue);
        Assert.Equal(consumerAttribute.ExecptionRequeue, consumerOptions.ExecptionRequeue);
    }

    [Fact]
    public void ConsumerAttributeInfo()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer2));

        var serviceDescriptors = services.ToArray();
        var consumerService = serviceDescriptors.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<TestEvent1>));
        Assert.NotNull(consumerService);
        Assert.Equal(ServiceLifetime.Transient, consumerService.Lifetime);

        var hostedServices = serviceDescriptors.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);

        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
        Assert.Equal(typeof(DefaultConsumerHostService<Consumer2, TestEvent1>), consumerHostService.ImplementationType);
    }

    public class TestEvent1
    {
        public int Id { get; set; }
    }

    public class Consumer1 : IConsumer<TestEvent1>
    {
        public Task ExecuteAsync(EventBody<TestEvent1> message) => Task.CompletedTask;
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent1>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent1>? message) => Task.FromResult(true);
    }

    [Consumer("tes", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    public class Consumer2 : Consumer1, IConsumer<TestEvent1>
    {
    }
}
