using Maomi.MQ;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.MediatR;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class MediatRTypeFilterTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenInvalidEventMiddleware()
    {
        // Arrange
        var invalidMiddleware = typeof(string);

        // Act & Assert
        var exception = Assert.Throws<TypeLoadException>(() =>
            new MediatrTypeFilter(eventMiddleware: invalidMiddleware));
        Assert.Contains("is not a valid event middleware", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultMiddleware_WhenEventMiddlewareIsNull()
    {
        // Act
        var filter = new MediatrTypeFilter();

        // Assert
        Assert.Equal(typeof(DefaultMediatrEventMiddleware<>), filter.GetType().GetField("_eventMiddleware", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(filter));
    }

    [Fact]
    public void Filter_ShouldNotRegister_WhenTypeIsNotClassOrAbstract()
    {
        // Arrange
        var services = new ServiceCollection();
        var filter = new MediatrTypeFilter();

        // Act
        filter.Filter(services, typeof(IRequest));

        // Assert
        Assert.Empty(services);
    }

    [Fact]
    public void Filter_ShouldNotRegister_WhenTypeDoesNotHaveConsumerAttribute()
    {
        // Arrange
        var services = new ServiceCollection();
        var filter = new MediatrTypeFilter();

        // Act
        filter.Filter(services, typeof(NoConsumerAttributeRequest));

        // Assert
        Assert.Empty(services);
    }

    [Fact]
    public void Filter_ShouldRegisterConsumer_WhenValidType()
    {
        // Arrange
        var services = new ServiceCollection();
        var filter = new MediatrTypeFilter();
        var type = typeof(ValidConsumerRequest);

        // Act
        filter.Filter(services, type);

        // Assert
        Assert.Contains(services, s => s.ServiceType == typeof(IConsumer<ValidConsumerRequest>));
        Assert.Contains(services, s => s.ServiceType == typeof(IEventMiddleware<ValidConsumerRequest>));
    }

    [Fact]
    public void Build_ShouldReturnRegisteredConsumers()
    {
        // Arrange
        var services = new ServiceCollection();
        var filter = new MediatrTypeFilter();
        var type = typeof(ValidConsumerRequest);

        filter.Filter(services, type);

        // Act
        var consumers = filter.Build(services);

        // Assert
        Assert.Single(consumers);
        Assert.Equal("TestQueue", consumers.First().Queue);
    }

    private class NoConsumerAttributeRequest : IRequest { }

    [Consumer("TestQueue")]
    private class ValidConsumerRequest : IRequest { }
}
