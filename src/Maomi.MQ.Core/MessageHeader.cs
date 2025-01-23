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
public class MessageHeader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHeader"/> class.
    /// </summary>
    public MessageHeader()
    {
        Id = Guid.NewGuid().ToString("N");
        Timestamp = DateTimeOffset.Now;
    }

    /// <summary>
    /// Message id.
    /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    /// The time when the message was created.<br />
    /// 消息被创建的时间.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The message comes with an attribute, which for RabbitMQ is IBasicProperties.<br />
    /// 消息附带属性，对于 RabbitMQ 是 IBasicProperties.
    /// </summary>
    public object Properties { get; init; } = null!;

    /// <summary>
    /// The content format of the message,ex: "application/json".
    /// </summary>
    public string ContentType { get; init; }

    /// <summary>
    /// Encoding of message,ex: "UTF-8".
    /// </summary>
    public string ContentEncoding { get; init; }

    /// <summary>
    /// The message type,ex: "order".
    /// </summary>
    public string Type { get; init; }

    /// <summary>
    /// The message is sent by which user.<br />
    /// </summary>
    public string UserId { get; init; }

    /// <summary>
    /// The message is sent by which application.<br />
    /// </summary>
    public string AppId { get; init; }
}
