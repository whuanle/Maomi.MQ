using AutoFixture.Xunit2;
using RabbitMQ.Client;

namespace Maomi.MQ.Tests;

public class MessageHeaderExtensionsTests
{
    [Theory, AutoData]
    public void GetMessageHeader_ShouldReturnCorrectMessageHeader(BasicProperties properties)
    {
        var result = properties.GetMessageHeader();

        Assert.Equal(properties.MessageId, result.Id);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(properties.Timestamp.UnixTime), result.Timestamp);
        Assert.Equal(properties.ContentType, result.ContentType);
        Assert.Equal(properties.ContentEncoding, result.ContentEncoding);
        Assert.Equal(properties.Type, result.Type);
        Assert.Equal(properties.UserId, result.UserId);
        Assert.Equal(properties.AppId, result.AppId);
        Assert.Equal(properties, result.Properties);
    }
}
