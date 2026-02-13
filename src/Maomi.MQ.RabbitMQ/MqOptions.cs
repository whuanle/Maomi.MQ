// <copyright file="MqOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Global configuration. <br />
/// 全局配置.
/// </summary>
public class MqOptions
{
    /// <summary>
    /// 0-64.
    /// </summary>
    public int WorkId { get; init; }

    /// <summary>
    /// App name.
    /// </summary>
    public string AppName { get; init; } = null!;

    /// <summary>
    /// Create queues on startup,see <see cref="RabbitMQ.Client.IChannel.QueueDeclareAsync"/>.<br />
    /// 是否自动创建队列.
    /// </summary>
    public bool AutoQueueDeclare { get; init; } = true;

    /// <summary>
    /// RabbitMQ connection factory.
    /// </summary>
    public IConnectionFactory ConnectionFactory { get; init; } = null!;

    /// <summary>
    /// Message serializers.<br />
    /// 消息序列化器，每个 ContentType 对应一个序列化器.
    /// </summary>
    public IReadOnlyCollection<IMessageSerializer> MessageSerializers { get; init; } = Array.Empty<IMessageSerializer>();
}
