// <copyright file="TransactionBarrierService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using System.Data;
using System.Data.Common;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Default implementation of <see cref="ITransactionBarrierService"/>.
/// </summary>
public sealed class TransactionBarrierService : ITransactionBarrierService
{
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IDatabaseProvider _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBarrierService"/> class.
    /// </summary>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="databaseProvider">Database provider.</param>
    public TransactionBarrierService(IMQTransactionOptions transactionOptions, IDatabaseProvider databaseProvider)
    {
        _transactionOptions = transactionOptions;
        _databaseProvider = databaseProvider;
    }

    /// <inheritdoc/>
    public async Task<TransactionBarrierContext> EnterAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        string consumerName,
        MessageHeader messageHeader,
        CancellationToken cancellationToken = default)
    {
        ValidateConnectionAndTransaction(dbConnection, dbTransaction);

        if (string.IsNullOrWhiteSpace(consumerName))
        {
            throw new ArgumentException("Consumer name is required.", nameof(consumerName));
        }

        if (string.IsNullOrWhiteSpace(messageHeader.Id))
        {
            throw new ArgumentException("Message header id is required.", nameof(messageHeader));
        }

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var lockId = _transactionOptions.NodeId;

        var barrier = new InboxBarrierEntity
        {
            ConsumerName = consumerName,
            MessageId = messageHeader.Id,
            MessageHeader = System.Text.Json.JsonSerializer.Serialize(messageHeader, _transactionOptions.JsonSerializerOptions),
            Exchange = messageHeader.Exchange ?? string.Empty,
            RoutingKey = messageHeader.RoutingKey ?? string.Empty,
            Status = (int)MessageStatus.Pending,
            LockId = lockId,
            LockTime = now,
            LastError = string.Empty,
            CreateTime = now,
            UpdateTime = now
        };

        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        var enterResult = await _databaseProvider.TryEnterInboxBarrierAsync(
            command,
            barrier,
            _transactionOptions.Consumer.ProcessingTimeout,
            cancellationToken);

        return new TransactionBarrierContext
        {
            ConsumerName = consumerName,
            MessageId = messageHeader.Id,
            LockId = lockId,
            EnterResult = enterResult
        };
    }

    /// <inheritdoc/>
    public async Task<bool> MarkSucceededAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TransactionBarrierContext context,
        CancellationToken cancellationToken = default)
    {
        ValidateConnectionAndTransaction(dbConnection, dbTransaction);
        ValidateBarrierContext(context);

        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        return await _databaseProvider.MarkInboxBarrierSucceededAsync(
            command,
            context.ConsumerName,
            context.MessageId,
            context.LockId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkFailedAsync(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TransactionBarrierContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        ValidateConnectionAndTransaction(dbConnection, dbTransaction);
        ValidateBarrierContext(context);
        ArgumentNullException.ThrowIfNull(exception);

        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        return await _databaseProvider.MarkInboxBarrierFailedAsync(
            command,
            context.ConsumerName,
            context.MessageId,
            context.LockId,
            DateTimeOffset.UtcNow,
            Truncate(exception.ToString(), _transactionOptions.Consumer.MaxErrorLength),
            cancellationToken);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static void ValidateBarrierContext(TransactionBarrierContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(context.ConsumerName))
        {
            throw new ArgumentException("Consumer name is required.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.MessageId))
        {
            throw new ArgumentException("Message id is required.", nameof(context));
        }

        if (string.IsNullOrWhiteSpace(context.LockId))
        {
            throw new ArgumentException("Lock id is required.", nameof(context));
        }
    }

    private static void ValidateConnectionAndTransaction(DbConnection dbConnection, DbTransaction dbTransaction)
    {
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(dbTransaction);

        if (!ReferenceEquals(dbConnection, dbTransaction.Connection))
        {
            throw new InvalidOperationException("The provided dbConnection does not match dbTransaction.Connection.");
        }
    }
}
