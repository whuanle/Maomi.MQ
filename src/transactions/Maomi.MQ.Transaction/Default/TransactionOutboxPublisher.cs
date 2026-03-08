// <copyright file="TransactionOutboxPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Data;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Default implementation of <see cref="ITransactionOutboxPublisher"/>.
/// </summary>
public sealed class TransactionOutboxPublisher : ITransactionOutboxPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IChannelMessagePublisher _publisher;
    private readonly ConnectionPool _connectionPool;
    private readonly ILogger<TransactionOutboxPublisher> _logger;
    private readonly OutboxMessageEntity _outbox;
    private readonly MessageHeader _messageHeader;
    private readonly byte[] _body;
    private readonly BasicProperties _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionOutboxPublisher"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="databaseProvider">Database provider.</param>
    /// <param name="publisher">Message publisher.</param>
    /// <param name="connectionPool">Connection pool.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="outbox">Outbox entity.</param>
    /// <param name="messageHeader">Message header.</param>
    /// <param name="body">Message body.</param>
    /// <param name="properties">Message properties.</param>
    public TransactionOutboxPublisher(
        IServiceProvider serviceProvider,
        IMQTransactionOptions transactionOptions,
        IDatabaseProvider databaseProvider,
        IChannelMessagePublisher publisher,
        ConnectionPool connectionPool,
        ILogger<TransactionOutboxPublisher> logger,
        OutboxMessageEntity outbox,
        MessageHeader messageHeader,
        byte[] body,
        BasicProperties properties)
    {
        _serviceProvider = serviceProvider;
        _transactionOptions = transactionOptions;
        _databaseProvider = databaseProvider;
        _publisher = publisher;
        _connectionPool = connectionPool;
        _logger = logger;
        _outbox = outbox;
        _messageHeader = messageHeader;
        _body = body;
        _properties = properties;
    }

    /// <inheritdoc/>
    public long MessageId => _outbox.MessageId;

    /// <inheritdoc/>
    public string Exchange => _outbox.Exchange;

    /// <inheritdoc/>
    public string RoutingKey => _outbox.RoutingKey;

    /// <inheritdoc/>
    public DateTimeOffset CreateTime => _outbox.CreateTime;

    /// <inheritdoc/>
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _transactionOptions.Connection(_serviceProvider);
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var dispatchLockId = $"{_transactionOptions.NodeId}:{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;

        using (var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            using var lockCommand = connection.CreateCommand();
            lockCommand.Transaction = tx;
            var acquired = await _databaseProvider.TryLockOutboxAsync(
                lockCommand,
                _outbox.MessageId,
                dispatchLockId,
                now,
                cancellationToken);

            if (!acquired)
            {
                tx.Rollback();
                return;
            }

            tx.Commit();
        }

        try
        {
            await _publisher.PublishChannelAsync(
                channel: _connectionPool.Get().DefaultChannel,
                exchange: _outbox.Exchange,
                routingKey: _outbox.RoutingKey,
                messageHeader: _messageHeader,
                message: _body,
                properties: _properties,
                cancellationToken: cancellationToken);

            using var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            using var successCommand = connection.CreateCommand();
            successCommand.Transaction = tx;
            var marked = await _databaseProvider.MarkOutboxSucceededAsync(
                successCommand,
                _outbox.MessageId,
                dispatchLockId,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (!marked)
            {
                tx.Rollback();
                throw new InvalidOperationException($"Outbox message {_outbox.MessageId} was published but failed to mark as succeeded.");
            }

            tx.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Immediate outbox publish failed for message {MessageId}.", _outbox.MessageId);

            using var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            using var failCommand = connection.CreateCommand();
            failCommand.Transaction = tx;
            var delay = _transactionOptions.Publisher.RetryInterval(_serviceProvider, _outbox.RoutingKey, _messageHeader, _outbox.RetryCount + 1);
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            var nextRetryTime = DateTimeOffset.UtcNow.Add(delay);
            var updated = await _databaseProvider.MarkOutboxFailedAsync(
                failCommand,
                _outbox.MessageId,
                dispatchLockId,
                DateTimeOffset.UtcNow,
                nextRetryTime,
                Truncate(ex.ToString(), _transactionOptions.Publisher.MaxErrorLength),
                cancellationToken);

            if (!updated)
            {
                tx.Rollback();
                throw new InvalidOperationException($"Outbox message {_outbox.MessageId} failed to update retry state after publish exception.", ex);
            }

            tx.Commit();
            throw;
        }
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
