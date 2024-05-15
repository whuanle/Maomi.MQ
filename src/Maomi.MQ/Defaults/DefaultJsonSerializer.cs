using System;

namespace Maomi.MQ.Defaults;

/// <summary>
/// 默认序列化器.
/// </summary>
public class DefaultJsonSerializer : IJsonSerializer
{
    /// <inheritdoc />
    public TEvent? Deserialize<TEvent>(ReadOnlySpan<byte> bytes)
        where TEvent : class
    {
        return System.Text.Json.JsonSerializer.Deserialize<TEvent>(bytes);
    }

    /// <inheritdoc />
    public byte[] Serializer<TEvent>(TEvent obj)
    where TEvent : class
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
    }
}
