// <copyright file="IMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Serializer.<br />
/// 序列化消息.
/// </summary>
public interface IMessageSerializer
{
    /// <summary>
    /// MIME content type.<br />
    /// 消息的编码类型.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Verify whether this type of serialization should be used. <br />
    /// 验证类型是否该使用此序列化器.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <param name="obj">Object.</param>
    /// <returns><see cref="byte"/>[].</returns>
    public bool SerializerVerify<TObject>(TObject obj);

    /// <summary>
    /// Verify whether this type of serialization should be used. <br />
    /// 验证类型是否该使用此序列化器.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <returns><see cref="byte"/>[].</returns>
    public bool SerializerVerify<TObject>();

    /// <summary>
    /// Serializer.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <param name="obj">Object.</param>
    /// <returns><see cref="byte"/>[].</returns>
    public byte[] Serializer<TObject>(TObject obj);

    /// <summary>
    /// Deserialize.
    /// </summary>
    /// <typeparam name="TObject">Type.</typeparam>
    /// <param name="bytes"><see cref="byte"/>[].</param>
    /// <returns>TObject.</returns>
    public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes);
}
