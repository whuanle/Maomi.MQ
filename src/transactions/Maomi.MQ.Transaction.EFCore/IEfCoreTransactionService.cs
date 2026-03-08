// <copyright file="IEfCoreTransactionService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace Maomi.MQ.Transaction.EFCore;

/// <summary>
/// EF Core based transaction service for outbox and inbox barrier workflows.
/// </summary>
public interface IEfCoreTransactionService
{
    /// <summary>
    /// Registers outbox in current EF Core transaction by resolving exchange and routing key from message type.
    /// If current context has no transaction, a local transaction will be created and committed.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbContext">EF Core context.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional properties mutator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outbox publisher for post-commit publish.</returns>
    Task<ITransactionOutboxPublisher> RegisterAutoAsync<TMessage>(
        DbContext dbContext,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Registers outbox in current EF Core transaction with explicit route.
    /// If current context has no transaction, a local transaction will be created and committed.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbContext">EF Core context.</param>
    /// <param name="exchange">Exchange name.</param>
    /// <param name="routingKey">Routing key.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outbox publisher for post-commit publish.</returns>
    Task<ITransactionOutboxPublisher> RegisterAsync<TMessage>(
        DbContext dbContext,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Executes business logic and registers outbox in one EF Core transaction.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbContext">EF Core context.</param>
    /// <param name="businessAction">Business action executed before outbox registration.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional properties mutator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outbox publisher for post-commit publish.</returns>
    Task<ITransactionOutboxPublisher> ExecuteAndRegisterAutoAsync<TMessage>(
        DbContext dbContext,
        Func<DbContext, CancellationToken, Task> businessAction,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Executes business logic and registers outbox with explicit route in one EF Core transaction.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="dbContext">EF Core context.</param>
    /// <param name="businessAction">Business action executed before outbox registration.</param>
    /// <param name="exchange">Exchange name.</param>
    /// <param name="routingKey">Routing key.</param>
    /// <param name="message">Message payload.</param>
    /// <param name="properties">Optional properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outbox publisher for post-commit publish.</returns>
    Task<ITransactionOutboxPublisher> ExecuteAndRegisterAsync<TMessage>(
        DbContext dbContext,
        Func<DbContext, CancellationToken, Task> businessAction,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Executes consumer business logic in inbox barrier flow with EF Core transaction.
    /// </summary>
    /// <param name="dbContext">EF Core context.</param>
    /// <param name="consumerName">Consumer queue name.</param>
    /// <param name="messageHeader">Message header.</param>
    /// <param name="handler">Business handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteInBarrierAsync(
        DbContext dbContext,
        string consumerName,
        MessageHeader messageHeader,
        Func<DbContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
