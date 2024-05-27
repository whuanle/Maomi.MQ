// <copyright file="EventBody.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Diagnostics;
using System.Diagnostics;

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
    public string Queue { get; init; } = null!;

    /// <summary>
    /// 事件创建时间.
    /// </summary>
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>
    /// 事件体.
    /// </summary>
    public TEvent Body { get; init; } = default!;

    /// <summary>
    /// Get tags.
    /// </summary>
    /// <returns><see cref="IReadOnlyDictionary{string, object?}"/>.</returns>
    public virtual IReadOnlyDictionary<string, object?> GetHeaders()
    {
        return new Dictionary<string, object?>
        {
            { DiagnosticName.Event.Id, Id },
            { DiagnosticName.Event.CreateTime, CreateTime },
            { DiagnosticName.Event.Queue, Queue },
        };
    }

    /// <summary>
    /// Get tags.
    /// </summary>
    /// <returns><see cref="ActivityTagsCollection"/>.</returns>
    public virtual ActivityTagsCollection GetTags()
    {
        return new ActivityTagsCollection
        {
            { DiagnosticName.Event.Id, Id },
            { DiagnosticName.Event.CreateTime, CreateTime },
            { DiagnosticName.Event.Queue, Queue },
        };
    }
}
