// <copyright file="InterceptStatus.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Intercept status.
/// </summary>
public enum InterceptStatus
{
    /// <summary>
    /// Continue processing.
    /// </summary>
    Continue = 0,

    /// <summary>
    /// Ignore the message,ACK the message.
    /// </summary>
    ACK = 2,
}