using ProtoDemo.Proto;

namespace Maomi.MQ.Message.Protobuf.UnitTests;

public class ProtobufMessageSerializerTests
{
    private readonly ProtobufMessageSerializer _serializer = new();

    [Fact]
    public void ContentType_ShouldReturnApplicationXProtobuf()
    {
        Assert.Equal("application/x-protobuf", _serializer.ContentType);
    }

    [Fact]
    public void SerializerVerify_ValidProtobufType_ShouldReturnTrue()
    {
        Assert.True(_serializer.SerializerVerify<Person>());
        Assert.True(_serializer.SerializerVerify(new Person { Id = 1, Name = "Tom" }));
    }

    [Fact]
    public void SerializerVerify_InvalidType_ShouldReturnFalse()
    {
        Assert.False(_serializer.SerializerVerify<string>());
        Assert.False(_serializer.SerializerVerify("invalid"));
    }

    [Fact]
    public void SerializeDeserialize_ShouldRoundtrip()
    {
        var message = new Person
        {
            Id = 7,
            Name = "Alice",
            Email = "alice@example.com",
        };

        var bytes = _serializer.Serializer(message);
        var result = _serializer.Deserialize<Person>(bytes);

        Assert.NotNull(result);
        Assert.Equal(message.Id, result!.Id);
        Assert.Equal(message.Name, result.Name);
        Assert.Equal(message.Email, result.Email);
    }

    [Fact]
    public void Serialize_InvalidType_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => _serializer.Serializer("invalid"));
        Assert.Equal("obj", exception.ParamName);
    }

    [Fact]
    public void Deserialize_InvalidType_ShouldThrowArgumentException()
    {
        var bytes = new byte[] { 1, 2, 3 };

        var exception = Assert.Throws<ArgumentException>(() => _serializer.Deserialize<string>(bytes));
        Assert.Equal("String", exception.ParamName);
    }
}
