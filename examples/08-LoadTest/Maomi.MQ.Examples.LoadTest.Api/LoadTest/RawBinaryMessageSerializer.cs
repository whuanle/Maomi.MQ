using System.Text;

namespace Maomi.MQ.Samples.LoadTest;

public interface IRawBinaryPayload
{
}

public sealed class RawBinaryMessageSerializer : IMessageSerializer
{
    public string ContentType => "application/x-loadtest-raw";

    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!SerializerVerify<TObject>())
        {
            throw new ArgumentException("The object must implement IRawBinaryPayload.", typeof(TObject).Name);
        }

        using var memory = new MemoryStream(bytes.ToArray());
        using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: false);

        if (typeof(TObject) == typeof(RawBinaryLoadMessage))
        {
            var value = new RawBinaryLoadMessage
            {
                Sequence = reader.ReadInt64(),
                UnixTimeMilliseconds = reader.ReadInt64(),
                Payload = reader.ReadString()
            };

            return (TObject)(object)value;
        }

        throw new InvalidOperationException($"Unsupported raw message type: {typeof(TObject).FullName}");
    }

    public byte[] Serializer<TObject>(TObject obj)
    {
        if (obj is not RawBinaryLoadMessage value)
        {
            throw new ArgumentException("The object must be RawBinaryLoadMessage.", nameof(obj));
        }

        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory, Encoding.UTF8, leaveOpen: true);
        writer.Write(value.Sequence);
        writer.Write(value.UnixTimeMilliseconds);
        writer.Write(value.Payload ?? string.Empty);
        writer.Flush();

        return memory.ToArray();
    }

    public bool SerializerVerify<TObject>(TObject obj)
    {
        return obj is IRawBinaryPayload;
    }

    public bool SerializerVerify<TObject>()
    {
        return typeof(TObject).IsAssignableTo(typeof(IRawBinaryPayload));
    }
}
