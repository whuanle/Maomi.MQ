namespace Maomi.MQ.Message.Protobuf_net.UnitTests;

[ProtoBuf.ProtoContract]
public class ProtobufNetEvent
{
    [ProtoBuf.ProtoMember(1)]
    public int Id { get; set; }

    [ProtoBuf.ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoBuf.ProtoMember(3)]
    public bool Active { get; set; }
}
