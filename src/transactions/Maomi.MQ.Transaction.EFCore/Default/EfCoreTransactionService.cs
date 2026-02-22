// <copyright file="EfCoreTransactionService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RabbitMQ.Client;
using System.Data.Common;

namespace Maomi.MQ.Transaction.EFCore.Default;

/// <summary>
/// Default implementation of <see cref="IEfCoreTransactionService"/>.
/// </summary>
public sealed class EfCoreTransactionService : IEfCoreTransactionService
{
    private readonly ITransactionOutboxService _outboxService;
    private readonly ITransactionBarrierService _barrierService;
    private readonly EfCoreTransactionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreTransactionService"/> class.
    /// </summary>
    /// <param name="outboxService">Outbox service.</param>
    /// <param name="barrierService">Barrier service.</param>
    /// <param name="options">EF Core options.</param>
    public EfCoreTransactionService(
        ITransactionOutboxService outboxService,
        ITransactionBarrierService barrierService,
        EfCoreTransactionOptions options)
    {
        _outboxService = outboxService;
        _barrierService = barrierService;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task<ITransactionOutboxPublisher> RegisterAutoAsync<TMessage>(
        DbContext dbContext,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var scope = await EnsureTransactionAsync(dbContext, cancellationToken);
        try
        {
            var publisher = await _outboxService.RegisterAutoAsync(
                scope.Connection,
                scope.Transaction,
                message,
                properties,
                cancellationToken);

            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.CommitAsync(cancellationToken);
            }

            return publisher;
        }
        catch
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.DisposeAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ITransactionOutboxPublisher> RegisterAsync<TMessage>(
        DbContext dbContext,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var scope = await EnsureTransactionAsync(dbContext, cancellationToken);
        try
        {
            var publisher = await _outboxService.RegisterAsync(
                scope.Connection,
                scope.Transaction,
                exchange,
                routingKey,
                message,
                properties,
                cancellationToken);

            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.CommitAsync(cancellationToken);
            }

            return publisher;
        }
        catch
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.DisposeAsync();
            }
        }
    }

    /// <inheritdoc/>
    public Task<ITransactionOutboxPublisher> ExecuteAndRegisterAutoAsync<TMessage>(
        DbContext dbContext,
        Func<DbContext, CancellationToken, Task> businessAction,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return ExecuteAndRegisterCoreAsync(
            dbContext,
            businessAction,
            (connection, transaction, ct) => _outboxService.RegisterAutoAsync(connection, transaction, message, properties, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ITransactionOutboxPublisher> ExecuteAndRegisterAsync<TMessage>(
        DbContext dbContext,
        Func<DbContext, CancellationToken, Task> businessAction,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return ExecuteAndRegisterCoreAsync(
            dbContext,
            businessAction,
            (connection, transaction, ct) => _outboxService.RegisterAsync(connection, transaction, exchange, routingKey, message, properties, ct),
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ExecuteInBarrierAsync(
        DbContext dbContext,
        string consumerName,
        MessageHeader messageHeader,
        Func<DbContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        ArgumentNullException.ThrowIfNull(handler);

        var scope = await EnsureTransactionAsync(dbContext, cancellationToken);
        TransactionBarrierContext? barrier = null;
        try
        {
            barrier = await _barrierService.EnterAsync(
                scope.Connection,
                scope.Transaction,
                consumerName,
                messageHeader,
                cancellationToken);

            if (barrier.EnterResult != InboxBarrierEnterResult.Entered)
            {
                if (scope.OwnsTransaction)
                {
                    await scope.ContextTransaction!.CommitAsync(cancellationToken);
                }

                return;
            }

            await handler(dbContext, cancellationToken);
            if (_options.AutoSaveChanges)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var updated = await _barrierService.MarkSucceededAsync(
                scope.Connection,
                scope.Transaction,
                barrier,
                cancellationToken);

            if (!updated)
            {
                throw new InvalidOperationException($"Failed to mark inbox barrier as succeeded for consumer [{consumerName}] message [{barrier.MessageId}].");
            }

            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.CommitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (barrier != null && barrier.EnterResult == InboxBarrierEnterResult.Entered)
            {
                try
                {
                    await _barrierService.MarkFailedAsync(
                        scope.Connection,
                        scope.Transaction,
                        barrier,
                        ex,
                        cancellationToken);
                }
                catch
                {
                }
            }

            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.DisposeAsync();
            }
        }
    }

    private async Task<ITransactionOutboxPublisher> ExecuteAndRegisterCoreAsync(
        DbContext dbContext,
        Func<DbContext, CancellationToken, Task> businessAction,
        Func<DbConnection, DbTransaction, CancellationToken, Task<ITransactionOutboxPublisher>> register,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(businessAction);
        ArgumentNullException.ThrowIfNull(register);

        var scope = await EnsureTransactionAsync(dbContext, cancellationToken);
        try
        {
            await businessAction(dbContext, cancellationToken);
            if (_options.AutoSaveChanges)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var publisher = await register(scope.Connection, scope.Transaction, cancellationToken);

            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.CommitAsync(cancellationToken);
            }

            return publisher;
        }
        catch
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (scope.OwnsTransaction)
            {
                await scope.ContextTransaction!.DisposeAsync();
            }
        }
    }

    private static async Task<EfCoreTransactionScope> EnsureTransactionAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var db = dbContext.Database;
        var current = db.CurrentTransaction;
        if (current != null)
        {
            var currentTx = current.GetDbTransaction();
            var currentConn = currentTx.Connection
                ?? throw new InvalidOperationException("Current EF Core transaction does not have an active DbConnection.");
            return new EfCoreTransactionScope(currentConn, currentTx, null, false);
        }

        var contextTx = await db.BeginTransactionAsync(cancellationToken);
        var tx = contextTx.GetDbTransaction();
        var conn = tx.Connection
            ?? throw new InvalidOperationException("New EF Core transaction does not have an active DbConnection.");
        return new EfCoreTransactionScope(conn, tx, contextTx, true);
    }

    private sealed record EfCoreTransactionScope(
        DbConnection Connection,
        DbTransaction Transaction,
        IDbContextTransaction? ContextTransaction,
        bool OwnsTransaction);
}
