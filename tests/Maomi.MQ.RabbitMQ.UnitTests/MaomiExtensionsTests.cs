using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.Filters;
using Maomi.MQ.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maomi.MQ.RabbitMQ.UnitTests;

public class MaomiExtensionsTests
{
    [Fact]
    public void AddMaomiMQ_ShouldThrow_WhenAssemblyContainsDuplicateConsumerQueue()
    {
        var services = new ServiceCollection();
        services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));

        Assert.Throws<ArgumentException>(() =>
            services.AddMaomiMQ(
                builder =>
                {
                    builder.AppName = "app";
                    builder.WorkId = 3;
                    builder.AutoQueueDeclare = true;
                    builder.Rabbit = f => f.HostName = "localhost";
                },
                [typeof(TestConsumer).Assembly],
                [new ConsumerTypeFilter()]));
    }

    [Fact]
    public void AddMaomiMQ_WithNullBuilder_ShouldThrow()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            MaomiExtensions.AddMaomiMQ(
                services,
                builder: null!,
                assemblies: [typeof(MaomiExtensionsTests).Assembly],
                typeFilters: [new ConsumerTypeFilter()]));
    }

    [Fact]
    public void AddMaomiMQ_WithoutRabbitConfiguration_ShouldThrow()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddMaomiMQ(
                builder =>
                {
                    builder.AppName = "app";
                    builder.WorkId = 1;
                },
                [typeof(MaomiExtensionsTests).Assembly],
                [new ConsumerTypeFilter()]));
    }

    [Fact]
    public void AddMaomiMQ_WithDuplicateQueueNamesAcrossFilters_ShouldThrow()
    {
        var services = new ServiceCollection();

        var filter1 = new FixedConsumerFilter("dup-queue");
        var filter2 = new FixedConsumerFilter("dup-queue");

        Assert.Throws<InvalidOperationException>(() =>
            services.AddMaomiMQ(
                builder =>
                {
                    builder.AppName = "app";
                    builder.WorkId = 1;
                    builder.Rabbit = f => f.HostName = "localhost";
                },
                [typeof(MaomiExtensionsTests).Assembly],
                [filter1, filter2]));
    }

    private sealed class FixedConsumerFilter : ITypeFilter
    {
        private readonly string _queue;

        public FixedConsumerFilter(string queue)
        {
            _queue = queue;
        }

        public void Filter(IServiceCollection services, Type type)
        {
        }

        public IEnumerable<ConsumerType> Build(IServiceCollection services)
        {
            yield return new ConsumerType
            {
                Queue = _queue,
                Consumer = typeof(TestConsumer),
                Event = typeof(TestMessage),
                ConsumerOptions = new ConsumerOptions { Queue = _queue },
            };
        }
    }

    [Consumer("consumer-queue")]
    private sealed class TestConsumer : IConsumer<TestMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, TestMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestMessage? message, Exception? ex) => Task.FromResult(ConsumerState.Ack);
    }

    private sealed class TestMessage
    {
    }
}
