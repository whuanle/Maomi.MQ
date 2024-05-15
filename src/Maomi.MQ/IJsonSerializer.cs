using System;

namespace Maomi.MQ
{
    /// <summary>
    /// 序列化消息.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// 序列化消息.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] Serializer<TEvent>(TEvent obj) where TEvent : class;

        /// <summary>
        /// 反序列化.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public TEvent? Deserialize<TEvent>(ReadOnlySpan<byte> bytes) where TEvent : class;
    }
}
