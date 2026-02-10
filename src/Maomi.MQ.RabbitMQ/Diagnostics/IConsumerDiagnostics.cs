// <copyright file="IConsumerDiagnostics.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Consumer diagnostics abstraction.
/// </summary>
public interface IConsumerDiagnostics
{
    /// <summary>
    /// Start consume diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="eventArgs"></param>
    /// <param name="consumerOptions"></param>
    /// <returns><see cref="Activity"/> instance.</returns>
    Activity? StartConsume(in MessageHeader messageHeader, BasicDeliverEventArgs eventArgs, IConsumerOptions consumerOptions);

    /// <summary>
    /// Stop consume diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="activity"></param>
    void StopConsume(in MessageHeader messageHeader, Activity? activity);

    /// <summary>
    /// Record consume exception diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exception"></param>
    /// <param name="activity"></param>
    void ExceptionConsume(in MessageHeader messageHeader, Exception exception, Activity? activity);

    /// <summary>
    /// Start execute diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <returns><see cref="Activity"/> instance.</returns>
    Activity? StartExecute(in MessageHeader messageHeader);

    /// <summary>
    /// Stop execute diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="activity"></param>
    void StopExecute(in MessageHeader messageHeader, Activity? activity);

    /// <summary>
    /// Record execute exception diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exception"></param>
    /// <param name="activity"></param>
    void ExceptionExecute(in MessageHeader messageHeader, Exception exception, Activity? activity);

    /// <summary>
    /// Start retry diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <returns><see cref="Activity"/> instance.</returns>
    Activity? StartRetry(in MessageHeader messageHeader);

    /// <summary>
    /// Stop retry diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="activity"></param>
    void StopRetry(in MessageHeader messageHeader, Activity? activity);

    /// <summary>
    /// Record retry exception diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exception"></param>
    /// <param name="activity"></param>
    void ExceptionRetry(in MessageHeader messageHeader, Exception exception, Activity? activity);

    /// <summary>
    /// Start fallback diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <returns><see cref="Activity"/> instance.</returns>
    Activity? StartFallback(in MessageHeader messageHeader);

    /// <summary>
    /// Stop fallback diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="fallbackState"></param>
    /// <param name="activity"></param>
    void StopFallback(in MessageHeader messageHeader, ConsumerState fallbackState, Activity? activity);

    /// <summary>
    /// Record fallback exception diagnostics.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="exception"></param>
    /// <param name="activity"></param>
    void ExceptionFallback(in MessageHeader messageHeader, Exception exception, Activity? activity);

    /// <summary>
    /// Record consume failure count.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="consumerOptions"></param>
    void RecordFail(in MessageHeader messageHeader, IConsumerOptions consumerOptions);
}
