// <copyright file="ITransactionOutboxService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;
using System.Data.Common;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Service for registering outbox messages inside a business transaction.
/// </summary>
public interface ITransactionOutboxService
{
    /// <summary>
    /// Registers an outbox row by resolving exchange and routing key from message type.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbConnection">Opened database connection.</param>
    /// <param name="dbTransaction">Current business transaction.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional message properties mutator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered outbox info.</returns>
    Task<ITransactionOutboxPublisher> RegisterAutoAsync<TMessage>(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Registers an outbox row with explicit exchange and routing key.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbConnection">Opened database connection.</param>
    /// <param name="dbTransaction">Current business transaction.</param>
    /// <param name="exchange">Exchange name.</param>
    /// <param name="routingKey">Routing key.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional message properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered outbox info.</returns>
    Task<ITransactionOutboxPublisher> RegisterAsync<TMessage>(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Marks an outbox row as succeeded after message is published manually.
    /// </summary>
    /// <param name="messageId">Message id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if row was updated.</returns>
    Task<bool> MarkAsSucceededAsync(long messageId, CancellationToken cancellationToken = default);
}
