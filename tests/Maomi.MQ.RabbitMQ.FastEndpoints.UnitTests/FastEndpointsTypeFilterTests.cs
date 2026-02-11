using FastEndpoints;
using Maomi.MQ.Consumer;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.FastEndpoints.UnitTests;

public class FastEndpointsTypeFilterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenEventMiddlewareIsInvalid()
    {
        // Arrange
        Type invalidMiddleware = typeof(string);

        // Act
        TypeLoadException exception = Assert.Throws<TypeLoadException>(() => new FastEndpointsTypeFilter(eventMiddleware: invalidMiddleware));

        // Assert
        Assert.Contains("is not a valid event middleware", exception.Message);
    }

    [Fact]
    public void Filter_ShouldRegisterConsumerAndMiddleware_WhenTypeIsValid()
    {
        // Arrange
        ServiceCollection services = new();
        FastEndpointsTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidFastEndpointsCommand));

        // Assert
        Assert.Contains(services, x => x.ServiceType == typeof(IConsumer<ValidFastEndpointsCommand>));
        Assert.Contains(services, x => x.ServiceType == typeof(IEventMiddleware<ValidFastEndpointsCommand>));
    }

    [Fact]
    public void Filter_ShouldUseInterceptorOverrideOptions()
    {
        // Arrange
        ServiceCollection services = new();
        ConsumerInterceptor interceptor = (options, _) =>
        {
            ConsumerOptions overridden = new()
            {
                Queue = options.Queue + "-override",
            };

            return (true, overridden);
        };

        FastEndpointsTypeFilter filter = new(interceptor);

        // Act
        filter.Filter(services, typeof(ValidFastEndpointsCommand));
        IReadOnlyCollection<ConsumerType> consumers = filter.Build(services).ToList();

        // Assert
        ConsumerType consumerType = Assert.Single(consumers);
        Assert.Equal("fe-queue-override", consumerType.Queue);
    }

    [Fact]
    public void Filter_ShouldIgnoreType_WhenNoAttribute()
    {
        // Arrange
        ServiceCollection services = new();
        FastEndpointsTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(NoConsumerAttributeCommand));

        // Assert
        Assert.Empty(services);
        Assert.Empty(filter.Build(services));
    }

    [Fact]
    public void Filter_ShouldThrow_WhenQueueDuplicated()
    {
        // Arrange
        ServiceCollection services = new();
        FastEndpointsTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidFastEndpointsCommand));

        // Assert
        Assert.Throws<ArgumentException>(() => filter.Filter(services, typeof(AnotherValidFastEndpointsCommand)));
    }

    [Fact]
    public void Build_ShouldReturnAllRegisteredConsumers()
    {
        // Arrange
        ServiceCollection services = new();
        FastEndpointsTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidFastEndpointsCommand));
        filter.Filter(services, typeof(UniqueFastEndpointsCommand));
        IReadOnlyCollection<ConsumerType> consumers = filter.Build(services).ToList();

        // Assert
        Assert.Equal(2, consumers.Count);
        Assert.Contains(consumers, x => x.Queue == "fe-queue");
        Assert.Contains(consumers, x => x.Queue == "fe-queue-2");
    }

    [FastEndpointsConsumer("fe-queue")]
    private sealed class ValidFastEndpointsCommand : ICommand
    {
        public Task ExecuteAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    [FastEndpointsConsumer("fe-queue")]
    private sealed class AnotherValidFastEndpointsCommand : ICommand
    {
        public Task ExecuteAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    [FastEndpointsConsumer("fe-queue-2")]
    private sealed class UniqueFastEndpointsCommand : ICommand
    {
        public Task ExecuteAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoConsumerAttributeCommand : ICommand
    {
        public Task ExecuteAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
