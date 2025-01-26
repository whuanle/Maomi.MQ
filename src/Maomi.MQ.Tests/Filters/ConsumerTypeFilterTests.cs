using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Filters.Tests;

public class ConsumerTypeFilterTests
{
    [Fact]
    public void Build_ShouldReturnConsumers()
    {
        var consumerTypeFilter = new ConsumerTypeFilter();
        var services = new ServiceCollection();

        var result = consumerTypeFilter.Build(services);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ShouldNotAddConsumerToServices_WhenTypeNoConsumerAttribute()
    {
        var consumerTypeFilter = new ConsumerTypeFilter();
        var services = new ServiceCollection();
        var consumerType = typeof(EmptyTestConsumer);

        consumerTypeFilter.Filter(services, consumerType);

        var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IConsumer<TestEvent>));
        Assert.Null(serviceDescriptor);
    }


    [Fact]
    public void Filter_ShouldAddConsumerToServices_WhenTypeIsValidConsumer()
    {
        var consumerTypeFilter = new ConsumerTypeFilter();
        var services = new ServiceCollection();
        var consumerType = typeof(TestConsumer);

        consumerTypeFilter.Filter(services, consumerType);

        var serviceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IConsumer<TestEvent>));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void Filter_ShouldThrowArgumentException_WhenQueueIsDuplicated()
    {
        var consumerTypeFilter = new ConsumerTypeFilter();
        var services = new ServiceCollection();
        var consumerType = typeof(TestConsumer);

        consumerTypeFilter.Filter(services, consumerType);

        Assert.Throws<ArgumentException>(() => consumerTypeFilter.Filter(services, consumerType));
    }

    [Fact]
    public void Filter_ShouldAddSomeServices()
    {
        var consumerTypeFilter = new ConsumerTypeFilter();
        var services = new ServiceCollection();
        var consumerType = typeof(TestConsumer);

        consumerTypeFilter.Filter(services, consumerType);
        consumerTypeFilter.Build(services);

        var serviceProvider = services.BuildServiceProvider();
        var consumer = serviceProvider.GetRequiredService<IConsumer<TestEvent>>();

        Assert.NotNull(consumer);
        Assert.IsType<TestConsumer>(consumer);
    }

    private class EmptyTestConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
        {
            throw new NotImplementedException();
        }
    }

    [Consumer("test")]
    private class TestConsumer : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
        {
            throw new NotImplementedException();
        }

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
        {
            throw new NotImplementedException();
        }
    }

    private class TestEvent { }
}
