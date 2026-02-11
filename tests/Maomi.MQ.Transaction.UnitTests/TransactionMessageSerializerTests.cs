using Maomi.MQ.Transaction.Default;
using RabbitMQ.Client;

namespace Maomi.MQ.Transaction.UnitTests;

public class TransactionMessageSerializerTests
{
    [Fact]
    public void GetSerializer_ShouldReturnMatchedSerializer()
    {
        var options = CreateMqOptions(new IMessageSerializer[]
        {
            new FooMessageSerializer(),
            new BarMessageSerializer(),
        });
        var selector = new TransactionMessageSerializer(options);

        var serializer = selector.GetSerializer(new FooMessage());

        Assert.IsType<FooMessageSerializer>(serializer);
    }

    [Fact]
    public void GetSerializer_ShouldThrow_WhenNoSerializerMatches()
    {
        var options = CreateMqOptions(new IMessageSerializer[]
        {
            new FooMessageSerializer(),
        });
        var selector = new TransactionMessageSerializer(options);

        Assert.Throws<InvalidOperationException>(() => selector.GetSerializer(new UnknownMessage()));
    }

    private static MqOptions CreateMqOptions(IReadOnlyCollection<IMessageSerializer> serializers)
    {
        return new MqOptions
        {
            WorkId = 1,
            AppName = "test-app",
            ConnectionFactory = new ConnectionFactory
            {
                HostName = "localhost",
            },
            MessageSerializers = serializers,
        };
    }

    private sealed class FooMessage
    {
        public int Id { get; set; }
    }

    private sealed class BarMessage
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class UnknownMessage
    {
    }

    private sealed class FooMessageSerializer : IMessageSerializer
    {
        public string ContentType => "application/foo";

        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
        {
            _ = bytes;
            return default;
        }

        public bool SerializerVerify<TObject>(TObject obj)
        {
            return obj is FooMessage;
        }

        public bool SerializerVerify<TObject>()
        {
            return typeof(TObject) == typeof(FooMessage);
        }

        public byte[] Serializer<TObject>(TObject obj)
        {
            _ = obj;
            return [1, 2, 3];
        }
    }

    private sealed class BarMessageSerializer : IMessageSerializer
    {
        public string ContentType => "application/bar";

        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
        {
            _ = bytes;
            return default;
        }

        public bool SerializerVerify<TObject>(TObject obj)
        {
            return obj is BarMessage;
        }

        public bool SerializerVerify<TObject>()
        {
            return typeof(TObject) == typeof(BarMessage);
        }

        public byte[] Serializer<TObject>(TObject obj)
        {
            _ = obj;
            return [4, 5, 6];
        }
    }
}
