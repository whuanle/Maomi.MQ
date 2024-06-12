// <copyright file="AutoQueueDeclare.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Create queues on startup,<see cref="RabbitMQ.Client.IChannel.QueueDeclareAsync"/>.<br />
/// 是否自动创建队列.
/// </summary>
public enum AutoQueueDeclare
{
    /// <summary>
    /// Auto.
    /// </summary>
    None,

    /// <summary>
    /// Enable.
    /// </summary>
    Enable,

    /// <summary>
    /// Disable.
    /// </summary>
    Disable
}