// <copyright file="ConsumerState.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer state.<br />
/// After receiving RabbitMQ messages, ACK, NACK, and put back to the queue are determined by status enumeration.<br />
/// 消费者状态.<br />
/// 接受 RabbitMQ 消息后，通过状态枚举确定进行 ACK、NACK 以及放回队列等.
/// </summary>
public enum ConsumerState
{
    /// <summary>
    /// ACK.
    /// </summary>
    Ack = 1,

    /// <summary>
    /// Immediately NACK and use the default configuration to set whether to put the message back on the queue.<br />
    /// 立即 NACK，并使用默认配置设置是否将消息放回队列.
    /// </summary>
    Nack = 1 << 1,

    /// <summary>
    /// Immediately NACK and put the message back on the queue.<br />
    /// 立即 NACK，并将消息放回队列.
    /// </summary>
    NackAndRequeue = 1 << 2,

    /// <summary>
    /// Immediately NACK, and the message will be removed from the server queue.<br />
    /// 立即 NACK，消息将会从服务器队列中移除.
    /// </summary>
    NackAndNoRequeue = 1 << 3,

    /// <summary>
    /// Exception.<br />
    /// 出现异常情况.
    /// </summary>
    Exception = 1 << 4
}
