using System;

namespace Maomi.MQ.Defaults
{
    public class DefaultJsonSerializer : IJsonSerializer
    {
        public TEvent? Deserialize<TEvent>(ReadOnlySpan<byte> bytes)
            where TEvent : class
        {
            return System.Text.Json.JsonSerializer.Deserialize<TEvent>(bytes);
        }

        public byte[] Serializer<TEvent>(TEvent obj)
        where TEvent : class
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }
}
