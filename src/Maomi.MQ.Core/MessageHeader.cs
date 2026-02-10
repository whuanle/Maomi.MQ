// <copyright file="MessageHeader.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Message identification.<br />
/// 消息标识.
/// </summary>
public struct MessageHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHeader"/> struct.
    /// </summary>
    public MessageHeader()
    {
#if NET9_0_OR_GREATER
        Id = Guid.CreateVersion7().ToString("N");
#else
        Id = Guid.NewGuid().ToString("N");
#endif
        Timestamp = DateTimeOffset.Now;
    }

    /// <summary>
    /// Message id.
    /// </summary>
    public string Id { get; init; } = default!;

    /// <summary>
    /// The time when the message was created.<br />
    /// 消息被创建的时间.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = default!;

    /// <summary>
    /// The message comes with an attribute, which for RabbitMQ is IReadOnlyBasicProperties.<br />
    /// 消息附带属性，对于 RabbitMQ 是 IReadOnlyBasicProperties.
    /// </summary>
    public object Properties { get; init; } = new object();

    /// <summary>
    /// The content format of the message,ex: "application/json".
    /// </summary>
    public string ContentType { get; init; } = default!;

    /// <summary>
    /// The object type,ex: "order".<br />
    /// 被序列化传递的对象类型的名称.
    /// </summary>
    public string Type { get; init; } = default!;

    /// <summary>
    /// The message is sent by which application.<br />
    /// </summary>
    public string AppId { get; init; } = default!;

    /// <summary>
    /// The exchange the message was originally published to.
    /// </summary>
    public string Exchange { get; init; } = default!;

    /// <summary>
    /// The routing key used when the message was originally published.
    /// </summary>
    public string RoutingKey { get; init; } = default!;
}
