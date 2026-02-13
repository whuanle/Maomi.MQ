// <copyright file="IConsumerOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer.<br />
/// 消费者配置.
/// </summary>
public interface IConsumerOptions : IEquatable<IConsumerOptions>
{
    /// <summary>
    /// Queue name.<br />
    /// 队列名称.
    /// </summary>
    string Queue { get; }

    /// <summary>
    /// Bind the death message exchange.<br />
    /// 绑定死信交换器.
    /// </summary>
    string? DeadExchange { get; }

    /// <summary>
    /// Bind the death message queue.<br />
    /// 绑定死信队列.
    /// </summary>
    string? DeadRoutingKey { get; }

    /// <summary>
    /// Queue message expiration time, in millimeters.<br />
    /// 队列消息过期时间，单位毫秒.
    /// </summary>
    int Expiration { get; }

    /// <summary>
    /// Qos,1-65535.
    /// </summary>
    ushort Qos { get; }

    /// <summary>
    /// Whether to return to the queue when the number of consumption failures reaches the condition.<br />
    /// 消费失败次数达到条件时，是否放回队列.
    /// </summary>
    bool RetryFaildRequeue { get; }

    /// <summary>
    /// Create queues on startup,<see cref="RabbitMQ.Client.IChannel.QueueDeclareAsync"/>.<br />
    /// 是否自动创建队列.
    /// </summary>
    AutoQueueDeclare AutoQueueDeclare { get; }

    /// <summary>
    /// Bind the exchange.<br />
    /// 绑定交换器.
    /// </summary>
    string? BindExchange { get; }

    /// <summary>
    /// Exchange type.<br />
    /// 交换器类型.
    /// </summary>
    ExchangeType ExchangeType { get; }

    /// <summary>
    /// Bind the routing key.<br />
    /// 绑定路由键.
    /// </summary>
    string? RoutingKey { get; }

    /// <summary>
    /// Broadcast mode.<br />
    /// 广播模式.
    /// </summary>
    bool IsBroadcast { get; }

    /// <summary>
    /// Creates a new object that is a copy of the current instance.
    /// </summary>
    /// <returns>A new object that is a copy of this instance.</returns>
    IConsumerOptions Clone();

    /// <summary>
    /// Copy from another object.
    /// </summary>
    /// <param name="options"></param>
    void CopyFrom(IConsumerOptions options);
}
