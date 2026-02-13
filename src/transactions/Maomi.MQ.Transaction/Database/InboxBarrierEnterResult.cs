// <copyright file="InboxBarrierEnterResult.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Inbox barrier enter result.
/// </summary>
public enum InboxBarrierEnterResult
{
    /// <summary>
    /// Entered successfully and can execute business logic.
    /// </summary>
    Entered = 0,

    /// <summary>
    /// Message has already been completed.
    /// </summary>
    AlreadyCompleted = 1,

    /// <summary>
    /// Message is currently being processed by another node.
    /// </summary>
    Busy = 2
}
