using Maomi.MQ.Attributes;

namespace Maomi.MQ.RabbitMQ.UnitTests.Attributes;

public class EventOrderAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetOrder()
    {
        var attribute = new EventOrderAttribute(7);
        Assert.Equal(7, attribute.Order);
    }
}
