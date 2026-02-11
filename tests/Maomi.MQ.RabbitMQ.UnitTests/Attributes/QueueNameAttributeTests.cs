using Maomi.MQ;

namespace Maomi.MQ.RabbitMQ.UnitTests.Attributes;

public class QueueNameAttributeTests
{
    [Fact]
    public void Constructor_WithRoutingKey_ShouldSetRoutingKeyOnly()
    {
        var attribute = new QueueNameAttribute("route-a");

        Assert.Equal("route-a", attribute.RoutingKey);
        Assert.Null(attribute.Exchange);
    }

    [Fact]
    public void Constructor_WithExchangeAndRoutingKey_ShouldSetBoth()
    {
        var attribute = new QueueNameAttribute("ex-a", "route-a");

        Assert.Equal("route-a", attribute.RoutingKey);
        Assert.Equal("ex-a", attribute.Exchange);
    }

    [Fact]
    public void Constructor_WithEmptyRoutingKey_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new QueueNameAttribute(string.Empty));
    }
}
