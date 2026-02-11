// <copyright file="MessageStatus.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Models;

/// <summary>
/// Indicate the sending or receiving status of a message.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Not processed.
    /// </summary>
    None = 0,

    /// <summary>
    /// have been sent.
    /// </summary>
    Sended = 1,

    /// <summary>
    /// The message has been received and confirmed correctly.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// Failure in handling.
    /// </summary>
    Failed = 3
}
