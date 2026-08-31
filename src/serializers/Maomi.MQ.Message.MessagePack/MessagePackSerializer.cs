// <copyright file="MessagePackSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MessagePack;
using System.Collections.Concurrent;

namespace Maomi.MQ;

/// <summary>
/// MessagePackSerializer serializer.
/// </summary>
public class MessagePackSerializer : IMessageSerializer
{
    private static readonly ConcurrentDictionary<Type, bool> MessagePackTypeCache = new();

    /// <inheritdoc />
    public string ContentType => "application/x-msgpack";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!SerializerVerify<TObject>())
        {
            throw new ArgumentException("The object must be a messagepack message.", typeof(TObject).Name);
        }

        return MessagePack.MessagePackSerializer.Deserialize<TObject>(bytes.ToArray());
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        if (!SerializerVerify(obj))
        {
            throw new ArgumentException("The object must be a messagepack message.", nameof(obj));
        }

        return MessagePack.MessagePackSerializer.Serialize(obj!);
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>(TObject obj)
    {
        if (obj is null)
        {
            return false;
        }

        return IsMessagePackType(obj.GetType());
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>()
    {
        return IsMessagePackType(typeof(TObject));
    }

    private static bool IsMessagePackType(Type type)
    {
        var modelType = Nullable.GetUnderlyingType(type) ?? type;
        return MessagePackTypeCache.GetOrAdd(modelType, static valueType =>
            valueType.IsDefined(typeof(MessagePackObjectAttribute), inherit: false) ||
            valueType.IsDefined(typeof(UnionAttribute), inherit: false));
    }
}
