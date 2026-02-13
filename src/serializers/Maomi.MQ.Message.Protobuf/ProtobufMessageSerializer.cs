// <copyright file="ProtobufMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Google.Protobuf;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Protobuf message serializer.
/// </summary>
public class ProtobufMessageSerializer : IMessageSerializer
{
    /// <inheritdoc />
    public string ContentType => "application/x-protobuf";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!SerializerVerify<TObject>())
        {
            throw new ArgumentException("The object must be a protobuf message.", typeof(TObject).Name);
        }

        return ProtobufHelper<TObject>.Deserialize(bytes);
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        if (obj is Google.Protobuf.IMessage message == true)
        {
            return message.ToByteArray();
        }

        throw new ArgumentException("The object must be a protobuf message.", nameof(obj));
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>(TObject obj)
    {
        return obj is Google.Protobuf.IMessage;
    }

    /// <inheritdoc/>
    public bool SerializerVerify<TObject>()
    {
        return typeof(TObject).IsAssignableTo(typeof(IMessage));
    }

    private static class ProtobufHelper<TObject>
    {
        public static TObject? Deserialize(ReadOnlySpan<byte> bytes)
        {
            return (TObject?)Parser.ParseFrom(bytes.ToArray());
        }

        private static readonly MessageParser Parser = CreateParser();

        private static MessageParser CreateParser()
        {
            var messageType = typeof(TObject);
            if (!messageType.IsAssignableTo(typeof(IMessage)))
            {
                throw new ArgumentException("The object must be a protobuf message.", typeof(TObject).Name);
            }

            var parserProperty = messageType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
            if (parserProperty == null)
            {
                throw new InvalidOperationException($"The protobuf parser was not found for type '{messageType.FullName}'.");
            }

            if (parserProperty.GetValue(null) is not MessageParser parser)
            {
                throw new InvalidOperationException($"The protobuf parser for type '{messageType.FullName}' is invalid.");
            }

            return parser;
        }
    }
}
