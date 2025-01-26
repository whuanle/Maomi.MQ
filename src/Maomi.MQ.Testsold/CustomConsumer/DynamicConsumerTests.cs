using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maomi.MQ.Tests.CustomConsumer;
public class DynamicConsumerTests : BaseHostTests
{
    [Fact]
    public void AddConsumer()
    {
        var services = Mock();
        CustomConsumerTypeFilter typeFilter = new();
        typeFilter.AddConsumer(typeof(MyConsumer1), new ConsumerOptions
        {
            Queue = "test1"
        });
        typeFilter.AddConsumer(typeof(MyConsumer2), new ConsumerOptions
        {
            Queue = "test2"
        });

        typeFilter.Build(services);

        var hostedServices = services.Where(x => x.ServiceType == typeof(IHostedService)).ToArray();

        // + IWaitReadyFactory, IFirstHostService
        Assert.Equal(4, hostedServices.Length);
    }

    private class TestEvent
    {

    }

    private class MyConsumer1 : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FallbackAsync(EventBody<TestEvent>? message)
        {
            throw new NotImplementedException();
        }
    }
    private class MyConsumer2 : IConsumer<TestEvent>
    {
        public Task ExecuteAsync(EventBody<TestEvent> message)
        {
            throw new NotImplementedException();
        }

        public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FallbackAsync(EventBody<TestEvent>? message)
        {
            throw new NotImplementedException();
        }
    }
}
