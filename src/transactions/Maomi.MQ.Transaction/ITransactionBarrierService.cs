// <copyright file="ITransactionBarrierService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using System.Data.Common;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Inbox barrier service for manual consumer transaction flow.
/// </summary>
public interface ITransactionBarrierService
{
    /// <summary>
    /// Tries to enter inbox barrier in caller transaction.
    /// </summary>
    /// <param name="dbConnection">Opened database connection.</param>
    /// <param name="dbTransaction">Current business transaction.</param>
    /// <param name="consumerName">Consumer name or queue name.</param>
    /// <param name="messageHeader">Message header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Barrier context.</returns>
    Task<TransactionBarrierContext> EnterAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        string consumerName,
        MessageHeader messageHeader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks barrier row as succeeded in caller transaction.
    /// </summary>
    /// <param name="dbConnection">Opened database connection.</param>
    /// <param name="dbTransaction">Current business transaction.</param>
    /// <param name="context">Barrier context from <see cref="EnterAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkSucceededAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TransactionBarrierContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks barrier row as failed in caller transaction.
    /// </summary>
    /// <param name="dbConnection">Opened database connection.</param>
    /// <param name="dbTransaction">Current business transaction.</param>
    /// <param name="context">Barrier context from <see cref="EnterAsync"/>.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkFailedAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TransactionBarrierContext context,
        Exception exception,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Barrier context returned by <see cref="ITransactionBarrierService"/>.
/// </summary>
public sealed class TransactionBarrierContext
{
    /// <summary>
    /// Gets or sets consumer name.
    /// </summary>
    public string ConsumerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets message id.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets lock id.
    /// </summary>
    public string LockId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets enter result.
    /// </summary>
    public InboxBarrierEnterResult EnterResult { get; set; }
}
