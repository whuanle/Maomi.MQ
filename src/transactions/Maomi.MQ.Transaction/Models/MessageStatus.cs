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
    /// Not processed yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message is being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// The message has been completed successfully.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Failure in handling.
    /// </summary>
    Failed = 3
}
