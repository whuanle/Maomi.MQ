using Maomi.MQ.Consumer;
using Maomi.MQ.EventBus;
using Maomi.MQ.MediatR;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.RabbitMQ.MediatR.UnitTests;

public class MediatRTypeFilterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenEventMiddlewareIsInvalid()
    {
        // Arrange
        Type invalidMiddleware = typeof(string);

        // Act
        TypeLoadException exception = Assert.Throws<TypeLoadException>(() => new MediatRTypeFilter(eventMiddleware: invalidMiddleware));

        // Assert
        Assert.Contains("is not a valid event middleware", exception.Message);
    }

    [Fact]
    public void Filter_ShouldRegisterConsumerAndMiddleware_WhenTypeIsValid()
    {
        // Arrange
        ServiceCollection services = new();
        MediatRTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidRequest));

        // Assert
        Assert.Contains(services, x => x.ServiceType == typeof(IConsumer<ValidRequest>));
        Assert.Contains(services, x => x.ServiceType == typeof(IEventMiddleware<ValidRequest>));
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

        MediatRTypeFilter filter = new(interceptor);

        // Act
        filter.Filter(services, typeof(ValidRequest));
        IReadOnlyCollection<ConsumerType> consumers = filter.Build(services).ToList();

        // Assert
        ConsumerType consumerType = Assert.Single(consumers);
        Assert.Equal("mediatr-queue-override", consumerType.Queue);
    }

    [Fact]
    public void Filter_ShouldIgnoreType_WhenNoAttribute()
    {
        // Arrange
        ServiceCollection services = new();
        MediatRTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(NoConsumerAttributeRequest));

        // Assert
        Assert.Empty(services);
        Assert.Empty(filter.Build(services));
    }

    [Fact]
    public void Filter_ShouldThrow_WhenQueueDuplicated()
    {
        // Arrange
        ServiceCollection services = new();
        MediatRTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidRequest));

        // Assert
        Assert.Throws<ArgumentException>(() => filter.Filter(services, typeof(AnotherValidRequest)));
    }

    [Fact]
    public void Build_ShouldReturnAllRegisteredConsumers()
    {
        // Arrange
        ServiceCollection services = new();
        MediatRTypeFilter filter = new();

        // Act
        filter.Filter(services, typeof(ValidRequest));
        filter.Filter(services, typeof(UniqueRequest));
        IReadOnlyCollection<ConsumerType> consumers = filter.Build(services).ToList();

        // Assert
        Assert.Equal(2, consumers.Count);
        Assert.Contains(consumers, x => x.Queue == "mediatr-queue");
        Assert.Contains(consumers, x => x.Queue == "mediatr-queue-2");
    }

    [MediatRConsumer("mediatr-queue")]
    private sealed class ValidRequest : IRequest
    {
    }

    [MediatRConsumer("mediatr-queue")]
    private sealed class AnotherValidRequest : IRequest
    {
    }

    [MediatRConsumer("mediatr-queue-2")]
    private sealed class UniqueRequest : IRequest
    {
    }

    private sealed class NoConsumerAttributeRequest : IRequest
    {
    }
}
