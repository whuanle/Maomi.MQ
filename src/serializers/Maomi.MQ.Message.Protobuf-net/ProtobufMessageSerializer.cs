// <copyright file="ProtobufMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using ProtoBuf;
using System.Collections.Concurrent;

namespace Maomi.MQ;

/// <summary>
/// Protobuf-net message serializer.
/// </summary>
public class ProtobufMessageSerializer : IMessageSerializer
{
    private static readonly ConcurrentDictionary<Type, bool> ProtoContractTypeCache = new();

    /// <inheritdoc />
    public string ContentType => "application/x-protobuf";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!ProtoContractVerifier<TObject>.IsProtoContract)
        {
            throw new ArgumentException("The object must be a protobuf-net message.", typeof(TObject).Name);
        }

        return ProtoBuf.Serializer.Deserialize<TObject>(bytes, default!, null);
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        if (!SerializerVerify(obj))
        {
            throw new ArgumentException("The object must be a protobuf-net message.", nameof(obj));
        }

        using var memoryStream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(memoryStream, obj!);
        return memoryStream.ToArray();
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>(TObject obj)
    {
        if (obj is null)
        {
            return false;
        }

        return IsProtoContractType(obj.GetType());
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>()
    {
        return ProtoContractVerifier<TObject>.IsProtoContract;
    }

    private static class ProtoContractVerifier<TObject>
    {
        public static readonly bool IsProtoContract = IsProtoContractType(typeof(TObject));
    }

    private static bool IsProtoContractType(Type type)
    {
        var modelType = Nullable.GetUnderlyingType(type) ?? type;
        return ProtoContractTypeCache.GetOrAdd(modelType, static valueType => valueType.IsDefined(typeof(ProtoContractAttribute), inherit: false));
    }
}
