// <copyright file="CleanupBackgroundService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace Maomi.MQ.Transaction.Backgroud;

/// <summary>
/// Cleanup service for succeeded outbox and inbox rows.
/// </summary>
public sealed class CleanupBackgroundService : BackgroundService
{
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CleanupBackgroundService> _logger;
    private DbConnection? _dbConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupBackgroundService"/> class.
    /// </summary>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="serviceScopeFactory">Scope factory.</param>
    /// <param name="logger">Logger.</param>
    public CleanupBackgroundService(
        IMQTransactionOptions transactionOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CleanupBackgroundService> logger)
    {
        _transactionOptions = transactionOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
            _dbConnection = null;
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cleanupOptions = _transactionOptions.Cleanup;
        if (!cleanupOptions.Enabled)
        {
            return;
        }

        var interval = cleanupOptions.ScanInterval <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(5)
            : cleanupOptions.ScanInterval;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction cleanup loop failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
        var dbConnection = await GetDbConnectionAsync(serviceProvider, cancellationToken);

        var cleanup = _transactionOptions.Cleanup;
        var batchSize = cleanup.DeleteBatchSize <= 0 ? 500 : cleanup.DeleteBatchSize;
        var keepCompletedDays = cleanup.KeepCompletedDays.GetValueOrDefault();
        var maxCompletedCount = cleanup.MaxCompletedCount.GetValueOrDefault();

        if (keepCompletedDays > 0)
        {
            var cutoffTime = DateTimeOffset.UtcNow.AddDays(-keepCompletedDays);
            await DeleteByTimeConditionAsync(databaseProvider, dbConnection, cutoffTime, batchSize, cancellationToken);
        }

        if (maxCompletedCount > 0)
        {
            await DeleteByCountConditionAsync(databaseProvider, dbConnection, maxCompletedCount, batchSize, cancellationToken);
        }
    }

    private async Task DeleteByTimeConditionAsync(
        IDatabaseProvider databaseProvider,
        DbConnection dbConnection,
        DateTimeOffset cutoffTime,
        int batchSize,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            using var tx = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            using var outboxCommand = dbConnection.CreateCommand();
            outboxCommand.Transaction = tx;
            using var inboxCommand = dbConnection.CreateCommand();
            inboxCommand.Transaction = tx;

            var deletedOutbox = await databaseProvider.DeleteSucceededOutboxBeforeAsync(outboxCommand, cutoffTime, batchSize, cancellationToken);
            var deletedInbox = await databaseProvider.DeleteSucceededInboxBeforeAsync(inboxCommand, cutoffTime, batchSize, cancellationToken);
            tx.Commit();

            if (deletedOutbox < batchSize && deletedInbox < batchSize)
            {
                return;
            }
        }
    }

    private async Task DeleteByCountConditionAsync(
        IDatabaseProvider databaseProvider,
        DbConnection dbConnection,
        long maxCompletedCount,
        int batchSize,
        CancellationToken cancellationToken)
    {
        using (var command = dbConnection.CreateCommand())
        {
            var outboxCompleted = await databaseProvider.CountSucceededOutboxAsync(command, cancellationToken);
            var outboxDelete = outboxCompleted > maxCompletedCount
                ? outboxCompleted / 2
                : 0;
            while (outboxDelete > 0)
            {
                var take = (int)Math.Min(batchSize, outboxDelete);
                using var tx = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                using var deleteCommand = dbConnection.CreateCommand();
                deleteCommand.Transaction = tx;
                var deleted = await databaseProvider.DeleteOldestSucceededOutboxAsync(deleteCommand, take, cancellationToken);
                tx.Commit();

                if (deleted <= 0)
                {
                    break;
                }

                outboxDelete -= deleted;
            }
        }

        using (var command = dbConnection.CreateCommand())
        {
            var inboxCompleted = await databaseProvider.CountSucceededInboxAsync(command, cancellationToken);
            var inboxDelete = inboxCompleted > maxCompletedCount
                ? inboxCompleted / 2
                : 0;
            while (inboxDelete > 0)
            {
                var take = (int)Math.Min(batchSize, inboxDelete);
                using var tx = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                using var deleteCommand = dbConnection.CreateCommand();
                deleteCommand.Transaction = tx;
                var deleted = await databaseProvider.DeleteOldestSucceededInboxAsync(deleteCommand, take, cancellationToken);
                tx.Commit();

                if (deleted <= 0)
                {
                    break;
                }

                inboxDelete -= deleted;
            }
        }
    }

    private async Task<DbConnection> GetDbConnectionAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (_dbConnection == null)
        {
            _dbConnection = _transactionOptions.Connection(serviceProvider);
        }

        if (_dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync(cancellationToken);
        }

        return _dbConnection;
    }
}
