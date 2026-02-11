namespace Maomi.MQ.Serialization.Benchmarks.Models;

[ProtoBuf.ProtoContract]
public sealed class ProtobufNetMessage
{
    [ProtoBuf.ProtoMember(1)]
    public int Id { get; set; }

    [ProtoBuf.ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoBuf.ProtoMember(3)]
    public string Email { get; set; } = string.Empty;
}
