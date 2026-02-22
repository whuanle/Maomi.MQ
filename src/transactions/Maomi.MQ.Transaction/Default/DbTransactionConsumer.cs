// <copyright file="DbTransactionConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Transactions;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Wraps <see cref="IDbTransactionConsumer{TMessage}"/> with inbox barrier.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public class DbTransactionConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly IDbTransactionConsumer<TMessage> _innerConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly ILogger<DbTransactionConsumer<TMessage>> _logger;
    private readonly string _consumerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTransactionConsumer{TMessage}"/> class.
    /// </summary>
    /// <param name="innerConsumer">Inner consumer.</param>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="databaseProvider">Database provider.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="consumerName">Consumer queue name.</param>
    public DbTransactionConsumer(
        IDbTransactionConsumer<TMessage> innerConsumer,
        IServiceProvider serviceProvider,
        IMQTransactionOptions transactionOptions,
        IDatabaseProvider databaseProvider,
        ILogger<DbTransactionConsumer<TMessage>> logger,
        string consumerName)
    {
        _innerConsumer = innerConsumer;
        _serviceProvider = serviceProvider;
        _transactionOptions = transactionOptions;
        _databaseProvider = databaseProvider;
        _logger = logger;
        _consumerName = consumerName;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
    {
        using var dbConnection = _transactionOptions.Connection(_serviceProvider);
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var lockId = _transactionOptions.NodeId;
        var messageId = MessageIdConverter.ParseRequired(messageHeader.Id, "MessageHeader.Id");
        var barrier = new InboxBarrierEntity
        {
            ConsumerName = _consumerName,
            MessageId = messageId,
            MessageHeader = TransactionMessageStorageSerializer.SerializeHeader(messageHeader, _transactionOptions.JsonSerializerOptions),
            Exchange = messageHeader.Exchange ?? string.Empty,
            RoutingKey = messageHeader.RoutingKey ?? string.Empty,
            Status = (int)MessageStatus.Pending,
            LockId = lockId,
            LockTime = now,
            CreateTime = now,
            UpdateTime = now
        };

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);

        using var command = dbConnection.CreateCommand();
        var enterResult = await _databaseProvider.TryEnterInboxBarrierAsync(
            command,
            barrier,
            _transactionOptions.Consumer.ProcessingTimeout);

        if (enterResult == InboxBarrierEnterResult.AlreadyCompleted)
        {
            scope.Complete();
            return;
        }

        if (enterResult == InboxBarrierEnterResult.Busy)
        {
            throw new InvalidOperationException($"Inbox barrier is busy for consumer [{_consumerName}] message [{messageHeader.Id}].");
        }

        try
        {
            await _innerConsumer.ExecuteAsync(messageHeader, message);

            using var updateCommand = dbConnection.CreateCommand();
            var updated = await _databaseProvider.MarkInboxBarrierSucceededAsync(
                updateCommand,
                _consumerName,
                messageId,
                lockId,
                DateTimeOffset.UtcNow);

            if (!updated)
            {
                throw new InvalidOperationException($"Failed to mark inbox barrier as succeeded for consumer [{_consumerName}] message [{messageHeader.Id}].");
            }

            scope.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process transaction consumer message {MessageId}, queue {Queue}.", messageHeader.Id, _consumerName);

            using var failCommand = dbConnection.CreateCommand();
            await _databaseProvider.MarkInboxBarrierFailedAsync(
                failCommand,
                _consumerName,
                messageId,
                lockId,
                DateTimeOffset.UtcNow,
                Truncate(ex.ToString(), _transactionOptions.Consumer.MaxErrorLength));

            throw;
        }
    }

    /// <inheritdoc/>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    {
        return _innerConsumer.FaildAsync(messageHeader, ex, retryCount, message);
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        return _innerConsumer.FallbackAsync(messageHeader, message, ex);
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
}

