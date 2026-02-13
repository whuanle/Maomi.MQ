// <copyright file="IDatabaseProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Data.Common;
using System.Text.Json;

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Database provider, used for operating databases.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Ensure that the required tables exist in the database.
    /// </summary>
    /// <param name="command"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task EnsureTablesExistAsync(DbCommand command);

    /// <summary>
    /// Get the message that has been received.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="messageId"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task<ConsumerEntity?> GetReceivedMessage(DbCommand command, string messageId);

    /// <summary>
    /// Get the message that has not been sent.
    /// </summary>
    /// <param name="command"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task<PublisherEntity?> GetUnSentMessage(DbCommand command);

    /// <summary>
    /// Insert a received message into the database.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="entity"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task InsertReceivedMessage(DbCommand command, ConsumerEntity entity);

    /// <summary>
    /// Insert an unsent message into the database.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="publisher"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task InsertUnSentMessage(DbCommand command, PublisherEntity publisher);

    /// <summary>
    /// Update the status of a received message in the database.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="entity"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task UpdateReceivedMessage(DbCommand command, ConsumerEntity entity);

    /// <summary>
    /// Update the status of an unsent message in the database.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="entity"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task UpdateUnSentMessage(DbCommand command, PublisherEntity entity);
}
