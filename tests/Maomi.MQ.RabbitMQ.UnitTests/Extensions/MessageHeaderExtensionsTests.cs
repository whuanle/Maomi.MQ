using Maomi.MQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Extensions;

public class MessageHeaderExtensionsTests
{
    [Fact]
    public void GetMessageHeader_FromBasicDeliverEventArgs_ShouldMapFields()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var props = new BasicProperties
        {
            MessageId = "id-1",
            Timestamp = new AmqpTimestamp(timestamp.ToUnixTimeMilliseconds()),
            ContentType = "application/json",
            Type = "Demo.Type",
            AppId = "app-a",
        };

        var eventArgs = new BasicDeliverEventArgs(
            "ctag",
            1,
            false,
            "ex-a",
            "route-a",
            props,
            new ReadOnlyMemory<byte>([1, 2]),
            CancellationToken.None);

        var header = eventArgs.GetMessageHeader();

        Assert.Equal("id-1", header.Id);
        Assert.Equal("application/json", header.ContentType);
        Assert.Equal("Demo.Type", header.Type);
        Assert.Equal("app-a", header.AppId);
        Assert.Equal("ex-a", header.Exchange);
        Assert.Equal("route-a", header.RoutingKey);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(props.Timestamp.UnixTime), header.Timestamp);
        Assert.Same(props, header.Properties);
    }

    [Fact]
    public void GetMessageHeader_FromReadOnlyBasicProperties_ShouldMapHeadersFallback()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var props = new BasicProperties
        {
            MessageId = "id-2",
            Timestamp = new AmqpTimestamp(timestamp.ToUnixTimeMilliseconds()),
            ContentType = "application/json",
            Type = "Demo.Type",
            AppId = "app-b",
            Headers = new Dictionary<string, object?>
            {
                ["exchange"] = "ex-b",
                ["routingKey"] = "route-b",
            }
        };

        var header = ((IReadOnlyBasicProperties)props).GetMessageHeader();

        Assert.Equal("id-2", header.Id);
        Assert.Equal("application/json", header.ContentType);
        Assert.Equal("Demo.Type", header.Type);
        Assert.Equal("app-b", header.AppId);
        Assert.Equal("ex-b", header.Exchange);
        Assert.Equal("route-b", header.RoutingKey);
        Assert.Same(props, header.Properties);
    }

    [Fact]
    public void GetMessageHeader_WhenNullFields_ShouldFallbackToEmptyString()
    {
        var props = new BasicProperties
        {
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
        };

        var header = ((IReadOnlyBasicProperties)props).GetMessageHeader();

        Assert.Equal(string.Empty, header.Id);
        Assert.Equal(string.Empty, header.ContentType);
        Assert.Equal(string.Empty, header.Type);
        Assert.Equal(string.Empty, header.AppId);
        Assert.Equal(string.Empty, header.Exchange);
        Assert.Equal(string.Empty, header.RoutingKey);
    }
}
