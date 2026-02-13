using Maomi.MQ;

namespace Maomi.MQ.UnitTests.Core;

public class MessageHeaderTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        var min = DateTimeOffset.UtcNow.AddMinutes(-1);
        var header = new MessageHeader();
        var max = DateTimeOffset.UtcNow.AddMinutes(1);

        Assert.False(string.IsNullOrWhiteSpace(header.Id));
        Assert.Equal(32, header.Id.Length);
        Assert.InRange(header.Timestamp.ToUniversalTime(), min, max);
        Assert.NotNull(header.Properties);
    }

    [Fact]
    public void InitProperties_ShouldKeepAssignedValues()
    {
        var now = DateTimeOffset.UtcNow;
        var properties = new object();

        var header = new MessageHeader
        {
            Id = "message-id",
            Timestamp = now,
            ContentType = "application/json",
            Type = "demo.type",
            AppId = "app-a",
            Exchange = "exchange-a",
            RoutingKey = "route-a",
            Properties = properties,
        };

        Assert.Equal("message-id", header.Id);
        Assert.Equal(now, header.Timestamp);
        Assert.Equal("application/json", header.ContentType);
        Assert.Equal("demo.type", header.Type);
        Assert.Equal("app-a", header.AppId);
        Assert.Equal("exchange-a", header.Exchange);
        Assert.Equal("route-a", header.RoutingKey);
        Assert.Same(properties, header.Properties);
    }
}
