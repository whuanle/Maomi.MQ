namespace Maomi.MQ.Message.MessagePack.UnitTests;

public class MessagePackMessageSerializerTests
{
    private readonly MessagePackSerializer _serializer = new();

    [Fact]
    public void ContentType_ShouldReturnApplicationXMsgpack()
    {
        Assert.Equal("application/x-msgpack", _serializer.ContentType);
    }

    [Fact]
    public void SerializerVerify_ValidMessagePackType_ShouldReturnTrue()
    {
        Assert.True(_serializer.SerializerVerify<MessagePackEvent>());
        Assert.True(_serializer.SerializerVerify(new MessagePackEvent { Id = 1, Name = "A" }));
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
        var source = new MessagePackEvent
        {
            Id = 12,
            Name = "demo",
            Tags = ["mq", "serializer"],
        };

        var bytes = _serializer.Serializer(source);
        var result = _serializer.Deserialize<MessagePackEvent>(bytes);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result!.Id);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.Tags, result.Tags);
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
