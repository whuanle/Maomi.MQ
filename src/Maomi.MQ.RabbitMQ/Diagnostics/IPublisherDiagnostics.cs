// <copyright file="IPublisherDiagnostics.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Diagnostics;

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Publisher diagnostics abstraction.
/// </summary>
public interface IPublisherDiagnostics
{
    /// <summary>
    /// Start publish diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <returns><see cref="Activity"/> instance.</returns>
    Activity? Start(in MessageHeader messageHeader, string exchange, string routingKey);

    /// <summary>
    /// Stop publish diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="activity"></param>
    void Stop(in MessageHeader messageHeader, string exchange, string routingKey, Activity? activity);

    /// <summary>
    /// Record publish exception diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="exception"></param>
    /// <param name="activity"></param>
    void Exception(in MessageHeader messageHeader, string exchange, string routingKey, Exception exception, Activity? activity);

    /// <summary>
    /// Record message size metrics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="size"></param>
    /// <param name="activity"></param>
    void RecordMessageSize(in MessageHeader messageHeader, string exchange, string routingKey, long size, Activity? activity);
}
