// <copyright file="IDatabaseProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Data.Common;

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Database provider for outbox and inbox barrier operations.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Ensures required tables and indexes exist.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureTablesExistAsync(DbCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an outbox message within caller transaction.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="message">Outbox message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InsertOutboxAsync(DbCommand command, OutboxMessageEntity message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to lock due outbox messages for processing.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="lockId">Current node lock id.</param>
    /// <param name="now">Current utc time.</param>
    /// <param name="lockTimeout">Lock timeout.</param>
    /// <param name="maxRetry">Maximum retry count.</param>
    /// <param name="take">Take count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lock count.</returns>
    Task<int> TryLockOutboxBatchAsync(
        DbCommand command,
        string lockId,
        DateTimeOffset now,
        TimeSpan lockTimeout,
        int maxRetry,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outbox messages locked by current lock id.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="lockId">Current node lock id.</param>
    /// <param name="take">Take count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outbox messages.</returns>
    Task<IReadOnlyList<OutboxMessageEntity>> GetLockedOutboxBatchAsync(
        DbCommand command,
        string lockId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks outbox message as succeeded.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="messageId">Message id.</param>
    /// <param name="lockId">Current node lock id.</param>
    /// <param name="now">Current utc time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkOutboxSucceededAsync(
        DbCommand command,
        string messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks outbox message as failed and schedules next retry.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="messageId">Message id.</param>
    /// <param name="lockId">Current node lock id.</param>
    /// <param name="now">Current utc time.</param>
    /// <param name="nextRetryTime">Next retry time.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkOutboxFailedAsync(
        DbCommand command,
        string messageId,
        string lockId,
        DateTimeOffset now,
        DateTimeOffset nextRetryTime,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries entering inbox barrier for a message.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="barrier">Inbox barrier row.</param>
    /// <param name="lockTimeout">Lock timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enter result.</returns>
    Task<InboxBarrierEnterResult> TryEnterInboxBarrierAsync(
        DbCommand command,
        InboxBarrierEntity barrier,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks inbox barrier as succeeded.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="consumerName">Consumer name.</param>
    /// <param name="messageId">Message id.</param>
    /// <param name="lockId">Current lock id.</param>
    /// <param name="now">Current utc time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkInboxBarrierSucceededAsync(
        DbCommand command,
        string consumerName,
        string messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks inbox barrier as failed.
    /// </summary>
    /// <param name="command">Database command.</param>
    /// <param name="consumerName">Consumer name.</param>
    /// <param name="messageId">Message id.</param>
    /// <param name="lockId">Current lock id.</param>
    /// <param name="now">Current utc time.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated.</returns>
    Task<bool> MarkInboxBarrierFailedAsync(
        DbCommand command,
        string consumerName,
        string messageId,
        string lockId,
        DateTimeOffset now,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
