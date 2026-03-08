using Maomi.MQ.Transaction.Default;
using RabbitMQ.Client;
using System.Text.Json;

namespace Maomi.MQ.Transaction.UnitTests;

public class TransactionMessageStorageSerializerTests
{
    [Fact]
    public void SerializeHeaderAndDeserializeHeader_ShouldKeepExtendedBasicProperties()
    {
        var properties = new BasicProperties
        {
            MessageId = "123",
            AppId = "app-a",
            ContentType = "application/json",
            Type = "order-created",
            Expiration = "30000",
            CorrelationId = "corr-1",
            Priority = 5,
            Headers = new Dictionary<string, object?>
            {
                ["trace-id"] = "abc",
                ["payload"] = new byte[] { 1, 2, 3 },
                ["x-delay"] = 1000,
            }
        };

        var header = new MessageHeader
        {
            Id = "123",
            AppId = "test-app",
            ContentType = "application/json",
            Type = "demo.message",
            Exchange = "demo.exchange",
            RoutingKey = "demo.key",
            Timestamp = DateTimeOffset.UtcNow,
            Properties = properties,
        };

        var json = TransactionMessageStorageSerializer.SerializeHeader(header, new JsonSerializerOptions());
        var restoredHeader = TransactionMessageStorageSerializer.DeserializeHeader(json, new JsonSerializerOptions());
        var restoredProperties = TransactionMessageStorageSerializer.CreateBasicProperties(restoredHeader);

        Assert.Contains("\"Id\":\"123\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Properties\":", json, StringComparison.Ordinal);
        Assert.Equal("123", restoredProperties.MessageId);
        Assert.Equal("30000", restoredProperties.Expiration);
        Assert.Equal((byte)5, restoredProperties.Priority);
        Assert.Equal("corr-1", restoredProperties.CorrelationId);
        Assert.Equal("abc", restoredProperties.Headers?["trace-id"]?.ToString());
        Assert.Equal(1000, Assert.IsType<int>(restoredProperties.Headers?["x-delay"]));
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(restoredProperties.Headers?["payload"]));
    }

    [Fact]
    public void SerializeAndDeserializeBody_ShouldRoundtrip()
    {
        var bytes = new byte[] { 8, 9, 10, 11 };

        var text = TransactionMessageStorageSerializer.SerializeBody(bytes);
        var result = TransactionMessageStorageSerializer.DeserializeBody(text);

        Assert.Equal(bytes, result);
    }
}
