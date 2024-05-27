// <copyright file="EventInfo.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event info.<br />
/// 事件信息.
/// </summary>
public class EventInfo
{
    /// <summary>
    /// Qos.
    /// </summary>
    public ushort Qos { get; internal set; }

    /// <summary>
    /// Queue name.
    /// </summary>
    public string Queue { get; internal set; } = null!;

    /// <summary>
    /// Bind the death message queue.<br />
    /// 绑定死信队列.
    /// </summary>
    public string? DeadQueue { get; set; }

    /// <summary>
    /// Event type.
    /// </summary>
    public Type EventType { get; internal set; } = null!;

    /// <summary>
    /// <see cref="IEventMiddleware{TEvent}"/>.
    /// </summary>
    public Type Middleware { get; internal set; } = null!;

    /// <summary>
    /// Group.
    /// </summary>
    public string? Group { get; internal set; }

    /// <summary>
    /// Whether to return to the queue when the number of consumption failures reaches the condition.<br />
    /// 消费失败次数达到条件时，是否放回队列.
    /// </summary>
    public bool RetryFaildRequeue { get; internal set; }

    /// <summary>
    /// Whether to put back to the queue when an exception occurs, such as a serialization error, rather than an exception occurred during consumption.
    /// 出现异常时是否放回队列，例如序列化错误等原因导致的，而不是消费时发生异常导致的.
    /// </summary>
    public bool ExecptionRequeue { get; internal set; }

    /// <summary>
    /// Event handler.
    /// </summary>
    public SortedDictionary<int, Type> Handlers { get; private set; } = new();
}
