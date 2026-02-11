using Maomi.MQ;

namespace Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;

internal sealed class FakeMessageSerializer : IMessageSerializer
{
    private readonly Func<object?, bool> _verify;

    public FakeMessageSerializer(string contentType, Func<object?, bool>? verify = null)
    {
        ContentType = contentType;
        _verify = verify ?? (_ => true);
    }

    public string ContentType { get; }

    public bool SerializerVerify<TObject>(TObject obj)
    {
        return _verify(obj);
    }

    public bool SerializerVerify<TObject>()
    {
        return _verify(typeof(TObject));
    }

    public byte[] Serializer<TObject>(TObject obj)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
    }

    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        return System.Text.Json.JsonSerializer.Deserialize<TObject>(bytes);
    }
}
