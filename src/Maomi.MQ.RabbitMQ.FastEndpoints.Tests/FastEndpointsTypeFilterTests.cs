using FastEndpoints;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Reflection;
using Xunit;

namespace Maomi.MQ.Tests;

public class FastEndpointsTypeFilterTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenInvalidEventMiddlewareProvided()
    {
        // Arrange
        var invalidMiddlewareType = typeof(string);

        // Act & Assert
        var exception = Assert.Throws<TypeLoadException>(() =>
            new FastEndpointsTypeFilter(eventMiddleware: invalidMiddlewareType));
        Assert.Contains("is not a valid event middleware", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultMiddleware_WhenNoMiddlewareProvided()
    {
        // Act
        var filter = new FastEndpointsTypeFilter();

        // Assert
        Assert.NotNull(filter);
    }

    [Fact]
    public void Filter_ShouldNotRegister_WhenTypeIsNotAssignableToEventOrCommand()
    {
        // Arrange
        var services = new ServiceCollection();
        var filter = new FastEndpointsTypeFilter();
        var invalidType = typeof(string);

        // Act
        filter.Filter(services, invalidType);

        // Assert
        Assert.Empty(services);
    }

    private class TestEvent : IEvent { }
}
