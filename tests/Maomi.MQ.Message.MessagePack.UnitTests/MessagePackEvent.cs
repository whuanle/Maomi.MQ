namespace Maomi.MQ.Message.MessagePack.UnitTests;

[global::MessagePack.MessagePackObject]
public class MessagePackEvent
{
    [global::MessagePack.Key(0)]
    public int Id { get; set; }

    [global::MessagePack.Key(1)]
    public string Name { get; set; } = string.Empty;

    [global::MessagePack.Key(2)]
    public string[] Tags { get; set; } = [];
}
