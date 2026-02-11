// <copyright file="DbTransactionConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Automatic registration uses DbTransactionConsumer to execute IDbTransactionConsumer.
/// </summary>
/// <typeparam name="TMessage">Message model.</typeparam>
public class DbTransactionConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly IDbTransactionConsumer<TMessage> _dbTransactionConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMQTransactionOptions _mqTransactionOptions;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly ILogger<DbTransactionConsumer<TMessage>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTransactionConsumer{TMessage}"/> class.
    /// </summary>
    /// <param name="dbTransactionConsumer"></param>
    /// <param name="databaseProvider"></param>
    /// <param name="mqTransactionOptions"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public DbTransactionConsumer(IDbTransactionConsumer<TMessage> dbTransactionConsumer, IServiceProvider serviceProvider, IMQTransactionOptions mqTransactionOptions, IDatabaseProvider databaseProvider, ILogger<DbTransactionConsumer<TMessage>> logger)
    {
        _dbTransactionConsumer = dbTransactionConsumer;
        _serviceProvider = serviceProvider;
        _mqTransactionOptions = mqTransactionOptions;
        _databaseProvider = databaseProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
    {
        using var dbConnection = _mqTransactionOptions.Connection.Invoke(_serviceProvider);
        if (dbConnection.State != System.Data.ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        using var tran = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
        {
            IsolationLevel = IsolationLevel.Serializable,
        });

        using var command = dbConnection.CreateCommand();

        var consumerEntity = await _databaseProvider.GetReceivedMessage(command, messageHeader.Id);

        if (consumerEntity != null && consumerEntity.status == (int)MessageStatus.Confirmed)
        {
            return;
        }

        if (consumerEntity == null)
        {
            consumerEntity = new ConsumerEntity
            {
                message_id = messageHeader.Id,
                message_header = System.Text.Json.JsonSerializer.Serialize(messageHeader, _mqTransactionOptions.JsonSerializerOptions),
                exchange = messageHeader.Exchange!,
                routing_key = messageHeader.RoutingKey!,
                status = (int)MessageStatus.None,
                create_time = DateTimeOffset.Now,
            };

            var cmd = dbConnection.CreateCommand();
            await _databaseProvider.InsertReceivedMessage(cmd, consumerEntity);
        }

        try
        {
            await _dbTransactionConsumer.ExecuteAsync(messageHeader, message);
            var cmd = dbConnection.CreateCommand();
            await _databaseProvider.UpdateReceivedMessage(cmd, consumerEntity);
            tran.Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId} with routing key {RoutingKey}.", messageHeader.Id, messageHeader.RoutingKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    {
        return _dbTransactionConsumer.FaildAsync(messageHeader, ex, retryCount, message);
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        return _dbTransactionConsumer.FallbackAsync(messageHeader, message, ex);
    }
}
