using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Maomi.MQ.Tests.TypeFilter;

public partial class ConsumerTypeFilterTests
{
    [Fact]
    public void ConsumerAttributeInfo()
    {
        var consumerAttribute = typeof(UseConsumer1).GetCustomAttribute<ConsumerAttribute>();
        Assert.NotNull(consumerAttribute);

        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UseConsumer1));

        var ioc = services.BuildServiceProvider();
        var consumerOptions = ioc.GetRequiredKeyedService<IConsumerOptions>(serviceKey: "test1");
        Assert.Equal(consumerAttribute, consumerOptions);
    }

    [Fact]
    public void Filter()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UseConsumer1));

        var consumerOptions = services.FirstOrDefault(x => x.ServiceType == typeof(IConsumerOptions));
        Assert.NotNull(consumerOptions);
        Assert.Equal(ServiceLifetime.Singleton, consumerOptions.Lifetime);
        Assert.Equal("test1", consumerOptions.ServiceKey);

        var consumerService = services.FirstOrDefault(x => x.ServiceType == typeof(IConsumer<TestEvent>));
        Assert.NotNull(consumerService);
        Assert.Equal(ServiceLifetime.Scoped, consumerService.Lifetime);
        Assert.Equal("test1", consumerService.ServiceKey);
    }

    [Fact]
    public void Filter_NotAttribute()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();

        typeFilter.Filter(services, typeof(BaseConsumer));

        Assert.Empty(services);
    }

    [Fact]
    public void Filter_Some()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(BaseConsumer));
        typeFilter.Filter(services, typeof(UseConsumer1));
        typeFilter.Filter(services, typeof(UseConsumer2));

        Assert.Equal(4, services.Count);

        var consumerOptionsList = services.Where(x => x.ServiceType == typeof(IConsumerOptions)).ToArray();
        Assert.Equal(2, consumerOptionsList.Length);

        Assert.Equal("test1", consumerOptionsList[0].ServiceKey);
        Assert.Equal("test2", consumerOptionsList[1].ServiceKey);

        var consumerList = services.Where(x => x.ServiceType == typeof(IConsumer<TestEvent>)).ToArray();
        Assert.Equal(2, consumerList.Length);

        Assert.Equal("test1", consumerList[0].ServiceKey);
        Assert.Equal("test2", consumerList[1].ServiceKey);

        Assert.Equal(typeof(UseConsumer1), consumerList[0].KeyedImplementationType);
        Assert.Equal(typeof(UseConsumer2), consumerList[1].KeyedImplementationType);

        Assert.Equal(ServiceLifetime.Scoped, consumerList[0].Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, consumerList[1].Lifetime);
    }

    [Fact]
    public void Filter_Repeated()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(BaseConsumer));
        typeFilter.Filter(services, typeof(UseConsumer1));
        typeFilter.Filter(services, typeof(UseConsumer2));

        typeFilter.Filter(services, typeof(BaseConsumer));
        typeFilter.Filter(services, typeof(UseConsumer1));
        typeFilter.Filter(services, typeof(UseConsumer2));

        Assert.Equal(4, services.Count);

        var consumerOptionsList = services.Where(x => x.ServiceType == typeof(IConsumerOptions)).ToArray();
        Assert.Equal(2, consumerOptionsList.Length);

        Assert.Equal("test1", consumerOptionsList[0].ServiceKey);
        Assert.Equal("test2", consumerOptionsList[1].ServiceKey);

        var consumerList = services.Where(x => x.ServiceType == typeof(IConsumer<TestEvent>)).ToArray();
        Assert.Equal(2, consumerList.Length);

        Assert.Equal("test1", consumerList[0].ServiceKey);
        Assert.Equal("test2", consumerList[1].ServiceKey);

        Assert.Equal(typeof(UseConsumer1), consumerList[0].KeyedImplementationType);
        Assert.Equal(typeof(UseConsumer2), consumerList[1].KeyedImplementationType);

        Assert.Equal(ServiceLifetime.Scoped, consumerList[0].Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, consumerList[1].Lifetime);
    }

    [Fact]
    public void Build()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(UseConsumer1));

        var hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Empty(hostedServices);

        typeFilter.Build(services);
        hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);

        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
    }

    [Fact]
    public void Build_Some()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(BaseConsumer));
        typeFilter.Filter(services, typeof(UseConsumer1));
        typeFilter.Filter(services, typeof(UseConsumer2));

        var hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Empty(hostedServices);

        typeFilter.Build(services);
        hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();
        Assert.Single(hostedServices);

        var consumerHostService = hostedServices.FirstOrDefault();
        Assert.NotNull(consumerHostService);
    }

    [Fact]
    public void Interceptor()
    {
        var services = new ServiceCollection();
        var typeFilter = new ConsumerTypeFilter(ConsumerInterceptor);
        typeFilter.Filter(services, typeof(BaseConsumer));
        typeFilter.Filter(services, typeof(UseConsumer1));
        typeFilter.Filter(services, typeof(UseConsumer2));

        Assert.Equal(4, services.Count);

        var consumerOptionsList = services.Where(x => x.ServiceType == typeof(IConsumerOptions)).ToArray();
        Assert.Equal(2, consumerOptionsList.Length);

        Assert.Equal("test1_abcd", consumerOptionsList[0].ServiceKey);
        Assert.Equal("test2_abcd", consumerOptionsList[1].ServiceKey);

        var consumerList = services.Where(x => x.ServiceType == typeof(IConsumer<TestEvent>)).ToArray();
        Assert.Equal(2, consumerList.Length);

        Assert.Equal("test1_abcd", consumerList[0].ServiceKey);
        Assert.Equal("test2_abcd", consumerList[1].ServiceKey);

        Assert.Equal(typeof(UseConsumer1), consumerList[0].KeyedImplementationType);
        Assert.Equal(typeof(UseConsumer2), consumerList[1].KeyedImplementationType);

        Assert.Equal(ServiceLifetime.Scoped, consumerList[0].Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, consumerList[1].Lifetime);
    }
}

public partial class ConsumerTypeFilterTests
{
    private class TestEvent
    {
        public int Id { get; set; }
    }

    private class BaseConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message) => Task.CompletedTask;
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message) => Task.CompletedTask;
        public Task<bool> FallbackAsync(EventBody<TestEvent>? message) => Task.FromResult(true);
    }

    [Consumer("test1", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    private class UseConsumer1 : BaseConsumer, IConsumer<TestEvent>
    {

    }

    [Consumer("test2", Qos = 1, RetryFaildRequeue = true, ExecptionRequeue = true)]
    private class UseConsumer2 : BaseConsumer, IConsumer<TestEvent>
    {
    }

    private bool ConsumerInterceptor(ConsumerAttribute consumerAttribute, Type consumerType)
    {
        consumerAttribute.Queue = consumerAttribute.Queue + "_abcd";
        return true;
    }
}