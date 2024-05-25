// <copyright file="EventBody.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Event meessage.<br />
/// 事件消息体.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
public class EventBody<TEvent>
{
    /// <summary>
    /// 事件唯一 id.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Queue.
    /// </summary>
    public string Queue { get; init; }

    /// <summary>
    /// 事件创建时间.
    /// </summary>
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>
    /// 事件体.
    /// </summary>
    public TEvent Body { get; init; } = default!;
}
