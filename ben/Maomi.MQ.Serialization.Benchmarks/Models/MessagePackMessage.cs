namespace Maomi.MQ.Serialization.Benchmarks.Models;

[global::MessagePack.MessagePackObject]
public sealed class MessagePackMessage
{
    [global::MessagePack.Key(0)]
    public int Id { get; set; }

    [global::MessagePack.Key(1)]
    public string Name { get; set; } = string.Empty;

    [global::MessagePack.Key(2)]
    public string Email { get; set; } = string.Empty;
}
