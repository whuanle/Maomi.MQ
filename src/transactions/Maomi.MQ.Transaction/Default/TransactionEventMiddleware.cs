// <copyright file="TransactionEventMiddleware.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Transactions;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Event middleware with inbox barrier.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public class TransactionEventMiddleware<TMessage> : IEventMiddleware<TMessage>
    where TMessage : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IConsumerTypeProvider _consumerTypeProvider;
    private readonly ILogger<TransactionEventMiddleware<TMessage>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionEventMiddleware{TMessage}"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="databaseProvider">Database provider.</param>
    /// <param name="consumerTypeProvider">Consumer type provider.</param>
    /// <param name="logger"></param>
    public TransactionEventMiddleware(
        IServiceProvider serviceProvider,
        IMQTransactionOptions transactionOptions,
        IDatabaseProvider databaseProvider,
        IConsumerTypeProvider consumerTypeProvider,
        ILogger<TransactionEventMiddleware<TMessage>> logger)
    {
        _serviceProvider = serviceProvider;
        _transactionOptions = transactionOptions;
        _databaseProvider = databaseProvider;
        _consumerTypeProvider = consumerTypeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(MessageHeader messageHeader, TMessage message, EventHandlerDelegate<TMessage> next)
    {
        var consumerName = ResolveConsumerQueueName();

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
            ConsumerName = consumerName,
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
            throw new InvalidOperationException($"Inbox barrier is busy for consumer [{consumerName}] message [{messageHeader.Id}].");
        }

        try
        {
            await next(messageHeader, message, CancellationToken.None);

            using var updateCommand = dbConnection.CreateCommand();
            var updated = await _databaseProvider.MarkInboxBarrierSucceededAsync(
                updateCommand,
                consumerName,
                messageId,
                lockId,
                DateTimeOffset.UtcNow);

            if (!updated)
            {
                throw new InvalidOperationException($"Failed to mark inbox barrier as succeeded for consumer [{consumerName}] message [{messageHeader.Id}].");
            }

            scope.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process event middleware message {MessageId}, event {EventType}.", messageHeader.Id, typeof(TMessage).FullName);

            using var failCommand = dbConnection.CreateCommand();
            await _databaseProvider.MarkInboxBarrierFailedAsync(
                failCommand,
                consumerName,
                messageId,
                lockId,
                DateTimeOffset.UtcNow,
                Truncate(ex.ToString(), _transactionOptions.Consumer.MaxErrorLength));

            throw;
        }
    }

    /// <inheritdoc/>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage? message)
    {
        _ = messageHeader;
        _ = ex;
        _ = retryCount;
        _ = message;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        _ = messageHeader;
        _ = message;
        _ = ex;
        return Task.FromResult(ConsumerState.NackAndRequeue);
    }

    private string ResolveConsumerQueueName()
    {
        var consumerType = _consumerTypeProvider.FirstOrDefault(x => x.Event == typeof(TMessage));
        if (consumerType != null)
        {
            return consumerType.Queue;
        }

        return typeof(TMessage).FullName ?? typeof(TMessage).Name;
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

