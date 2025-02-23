using Google.Protobuf;
using ProtoDemo.Proto;

namespace Maomi.MQ.Tests;

public class ProtobufMessageSerializerTests
{
    private readonly ProtobufMessageSerializer _serializer = new ProtobufMessageSerializer();

    [Fact]
    public void ContentEncoding_ShouldReturnUtf8()
    {
        Assert.Equal("UTF-8", _serializer.ContentEncoding);
    }

    [Fact]
    public void ContentType_ShouldReturnApplicationXProtobuf()
    {
        Assert.Equal("application/x-protobuf", _serializer.ContentType);
    }

    [Fact]
    public void Serialize_ShouldReturnByteArray()
    {
        var message = new Person { Id = 1, Name = "Test" };
        var result = _serializer.Serializer(message);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Deserialize_ShouldReturnProtobufMessage()
    {
        var message = new Person { Id = 1, Name = "Test" };
        var bytes = message.ToByteArray();
        var result = _serializer.Deserialize<Person>(bytes);
        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
        Assert.Equal(message.Name, result.Name);
    }

    [Fact]
    public void Deserialize_InvalidType_ShouldThrowArgumentException()
    {
        var bytes = new byte[] { 1, 2, 3 };
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize<string>(bytes));
    }

    [Fact]
    public void Serialize_InvalidType_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _serializer.Serializer("InvalidType"));
    }
}
