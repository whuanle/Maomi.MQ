using Thrift.Protocol;

namespace Maomi.MQ.Message.Thrift.UnitTests;

public class ThriftMessageSerializerTests
{
    private readonly ThriftMessageSerializer _serializer = new();

    [Fact]
    public void ContentType_ShouldReturnApplicationXThrift()
    {
        Assert.Equal("application/x-thrift", _serializer.ContentType);
    }

    [Fact]
    public void SerializerVerify_ValidThriftType_ShouldReturnTrue()
    {
        Assert.True(_serializer.SerializerVerify<ThriftEvent>());
        Assert.True(_serializer.SerializerVerify(new ThriftEvent { Id = 1, Name = "A" }));
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
        var source = new ThriftEvent
        {
            Id = 88,
            Name = "thrift",
        };

        var bytes = _serializer.Serializer(source);
        var result = _serializer.Deserialize<ThriftEvent>(bytes);

        Assert.NotNull(result);
        Assert.Equal(source.Id, result!.Id);
        Assert.Equal(source.Name, result.Name);
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
