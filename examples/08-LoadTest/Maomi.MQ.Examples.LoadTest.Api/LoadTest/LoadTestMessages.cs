using Maomi.MQ;
using Maomi.MQ.Attributes;
using MessagePack;
using ProtoBuf;

namespace Maomi.MQ.Samples.LoadTest;

public static class LoadTestRoutes
{
    public const string Json = "loadtest.json.queue";
    public const string ProtobufNet = "loadtest.protobufnet.queue";
    public const string MessagePack = "loadtest.messagepack.queue";
    public const string RawBinary = "loadtest.raw.queue";
}

[RouterKey(LoadTestRoutes.Json)]
public sealed class JsonLoadMessage
{
    public long Sequence { get; set; }

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    public string Payload { get; set; } = string.Empty;
}

[ProtoContract]
[RouterKey(LoadTestRoutes.ProtobufNet)]
public sealed class ProtobufNetLoadMessage
{
    [ProtoMember(1)]
    public long Sequence { get; set; }

    [ProtoMember(2)]
    public long UnixTimeMilliseconds { get; set; }

    [ProtoMember(3)]
    public string Payload { get; set; } = string.Empty;
}

[MessagePackObject]
[RouterKey(LoadTestRoutes.MessagePack)]
public sealed class MessagePackLoadMessage
{
    [Key(0)]
    public long Sequence { get; set; }

    [Key(1)]
    public long UnixTimeMilliseconds { get; set; }

    [Key(2)]
    public string Payload { get; set; } = string.Empty;
}

[RouterKey(LoadTestRoutes.RawBinary)]
public sealed class RawBinaryLoadMessage : IRawBinaryPayload
{
    public long Sequence { get; set; }

    public long UnixTimeMilliseconds { get; set; }

    public string Payload { get; set; } = string.Empty;
}
