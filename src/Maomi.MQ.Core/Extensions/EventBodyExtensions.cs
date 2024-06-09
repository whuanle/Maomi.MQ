// <copyright file="EventBodyExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Diagnostics;
using System.Diagnostics;

namespace Maomi.MQ;

/// <summary>
/// Extensions.
/// </summary>
public static class EventBodyExtensions
{
    /// <summary>
    /// Get tags.
    /// </summary>
    /// <param name="eventBody">Event model.</param>
    /// <typeparam name="T">Event type.</typeparam>
    /// <returns><see cref="ActivityTagsCollection"/>.</returns>
    public static ActivityTagsCollection GetTags<T>(this EventBody<T> eventBody)
    {
        return new ActivityTagsCollection
        {
            { DiagnosticName.Event.Id, eventBody.Id },
            { DiagnosticName.Event.Publisher, eventBody.Publisher },
            { DiagnosticName.Event.CreationTime, eventBody.CreationTime },
            { DiagnosticName.Event.Queue, eventBody.Queue },
        };
    }
}
