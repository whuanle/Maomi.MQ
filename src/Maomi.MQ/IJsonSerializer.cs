using System;

namespace Maomi.MQ
{
    public interface IJsonSerializer
    {
        public byte[] Serializer<TEvent>(TEvent obj) where TEvent : class;
        public TEvent? Deserialize<TEvent>(ReadOnlySpan<byte> bytes) where TEvent : class;
    }
}
