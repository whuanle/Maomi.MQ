// <copyright file="FeMQEvent{TEvent}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// Send FastEndpoints events to the message queue.
/// </summary>
/// <typeparam name="TEvent">Event.</typeparam>
public class FeMQEvent<TEvent> : IEvent
    where TEvent : IEvent
{
    /// <summary>
    /// Event.
    /// </summary>
    public TEvent Event { get; init; } = default!;
}
