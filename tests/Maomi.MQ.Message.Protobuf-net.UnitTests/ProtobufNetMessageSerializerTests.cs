namespace Maomi.MQ.Message.Protobuf_net.UnitTests;

public class ProtobufNetMessageSerializerTests
{
    private readonly ProtobufMessageSerializer _serializer = new();

    [Fact]
    public void ContentType_ShouldReturnApplicationXProtobuf()
    {
        Assert.Equal("application/x-protobuf", _serializer.ContentType);
    }

    [Fact]
    public void SerializerVerify_ValidProtoContractType_ShouldReturnTrue()
    {
        Assert.True(_serializer.SerializerVerify<ProtobufNetEvent>());
        Assert.True(_serializer.SerializerVerify(new ProtobufNetEvent { Id = 1, Name = "A" }));
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
        var source = new ProtobufNetEvent
        {
            Id = 23,
            Name = "protobuf-net",
            Active = true,
        };

        var bytes = _serializer.Serializer(source);
        var result = _serializer.Deserialize<ProtobufNetEvent>(bytes);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result!.Id);
        Assert.Equal(source.Name, result.Name);
        Assert.Equal(source.Active, result.Active);
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
