// <copyright file="ProtobufMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Google.Protobuf;
using System.Linq.Expressions;

namespace Maomi.MQ;

/// <summary>
/// Protobuf message serializer.
/// </summary>
public class ProtobufMessageSerializer : IMessageSerializer
{
    private static readonly Google.Protobuf.JsonFormatter JsonFormatter = new Google.Protobuf.JsonFormatter(new Google.Protobuf.JsonFormatter.Settings(true));

    /// <inheritdoc />
    public string ContentEncoding => "UTF-8";

    /// <inheritdoc />
    public string ContentType => "application/x-protobuf";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        if (!typeof(TObject).IsAssignableTo(typeof(Google.Protobuf.IMessage)))
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

    private static class ProtobufHelper
    {
        public static TMessage? DeserializeProtobuf<TMessage>(ReadOnlySpan<byte> bytes)
            where TMessage : class, Google.Protobuf.IMessage, new()
        {
            TMessage message = new TMessage();
            return message.Descriptor.Parser.ParseFrom(bytes.ToArray()) as TMessage;
        }
    }

    private static class ProtobufHelper<TObject>
    {
        private delegate TObject? DeserializeDelegate(ReadOnlySpan<byte> bytes);

        private static readonly DeserializeDelegate _deserialize;

        static ProtobufHelper()
        {
            if (!typeof(TObject).IsAssignableTo(typeof(IMessage)))
            {
                throw new ArgumentException("The object must be a protobuf message.", typeof(TObject).Name);
            }

            var bytesParameter = Expression.Parameter(typeof(ReadOnlySpan<byte>), "bytes");
            var deserializeMethod = typeof(ProtobufHelper)
                .GetMethod(nameof(ProtobufHelper.DeserializeProtobuf), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (deserializeMethod == null)
            {
                throw new InvalidOperationException("DeserializeProtobuf method not found.");
            }

            deserializeMethod = deserializeMethod.MakeGenericMethod(typeof(TObject));

            var method = Expression.Call(method: deserializeMethod, arguments: new Expression[] { bytesParameter });
            _deserialize = Expression.Lambda<DeserializeDelegate>(method, bytesParameter).Compile();
        }

        public static TObject? Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (!typeof(TObject).IsAssignableTo(typeof(Google.Protobuf.IMessage)))
            {
                throw new ArgumentException("The object must be a protobuf message.", typeof(TObject).Name);
            }

            return _deserialize(bytes);
        }
    }
}
