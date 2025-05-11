// <copyright file="MessageSerializerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Default;

/// <summary>
/// Message serializer factory.
/// </summary>
public class MessageSerializerFactory : IMessageSerializer, IMessageSerializerFactory
{
    /// <summary>
    /// Create serializer delegate.
    /// </summary>
    /// <param name="type"></param>
    /// <returns><see cref="IMessageSerializer"/>.</returns>
    public delegate IMessageSerializer CreateSerializer(Type type);

    /// <summary>
    /// Create deserializer delegate.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="messageHeader"></param>
    /// <returns><see cref="IMessageSerializer"/>.</returns>
    public delegate IMessageSerializer CreateDeserializer(Type type, MessageHeader messageHeader);

    private readonly CreateSerializer _serializer;
    private readonly CreateDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageSerializerFactory"/> class.
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="deserializer"></param>
    public MessageSerializerFactory(CreateSerializer serializer, CreateDeserializer deserializer)
    {
        _serializer = serializer;
        _deserializer = deserializer;
    }

    /// <inheritdoc />
    public string ContentEncoding => "UTF-8";

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes)
    {
        var serializer = GetMessageSerializer(typeof(TObject));
        return serializer.Deserialize<TObject>(bytes);
    }

    /// <inheritdoc />
    public byte[] Serializer<TObject>(TObject obj)
    {
        var serializer = GetMessageSerializer(typeof(TObject));
        return serializer.Serializer<TObject>(obj);
    }

    /// <inheritdoc />
    public IMessageSerializer GetMessageSerializer(Type type)
    {
        var serializer = _serializer(type);
        return serializer;
    }

    /// <inheritdoc />
    public IMessageSerializer GetMessageDeserializer(Type type, MessageHeader messageHeader)
    {
        var serializer = _serializer(type);
        return serializer;
    }
}
