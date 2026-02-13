// <copyright file="DBTransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Data.Common;
using System.Transactions;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Message publisher that writes outbox rows in local transaction.
/// </summary>
public class DBTransactionPublisher : DefaultMessagePublisher, IDBTransactionPublisher, ITransactionMessagePublisher
{
    private readonly bool _hasConnection;
    private readonly DbConnection? _dbConnection;
    private readonly DbTransaction? _dbTransaction;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly ITransactionMessageSerializer _serializerSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="DBTransactionPublisher"/> class.
    /// </summary>
    /// <param name="publisher">Base publisher.</param>
    /// <param name="dbConnection">Database connection.</param>
    /// <param name="dbTransaction">Database transaction.</param>
    internal DBTransactionPublisher(DefaultMessagePublisher publisher, DbConnection dbConnection, DbTransaction dbTransaction)
        : base(publisher)
    {
        _hasConnection = true;
        _dbConnection = dbConnection;
        _dbTransaction = dbTransaction;
        _databaseProvider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
        _transactionOptions = _serviceProvider.GetRequiredService<IMQTransactionOptions>();
        _serializerSelector = _serviceProvider.GetRequiredService<ITransactionMessageSerializer>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DBTransactionPublisher"/> class.
    /// </summary>
    /// <param name="publisher">Base publisher.</param>
    internal DBTransactionPublisher(DefaultMessagePublisher publisher)
        : base(publisher)
    {
        _databaseProvider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
        _transactionOptions = _serviceProvider.GetRequiredService<IMQTransactionOptions>();
        _serializerSelector = _serviceProvider.GetRequiredService<ITransactionMessageSerializer>();
    }

    /// <inheritdoc/>
    public override async Task PublishChannelAsync<TMessage>(IChannel channel, string exchange, string reoutingKey, TMessage message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        _ = channel;

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (properties == null)
        {
            properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        var serializer = _serializerSelector.GetSerializer(message);

        var messageId = _idGen.NextId().ToString();
        MessageHeader messageHeader = new MessageHeader
        {
            Id = messageId,
            Timestamp = DateTimeOffset.UtcNow,
            AppId = _mqOptions.AppName,
            ContentType = serializer.ContentType,
            Type = message.GetType().FullName ?? typeof(TMessage).FullName,
            Exchange = exchange,
            RoutingKey = reoutingKey,
            Properties = properties
        };

        InitializeMessageProperties<TMessage>(properties, ref messageHeader, serializer);
        var body = serializer.Serializer(message);

        var now = DateTimeOffset.UtcNow;
        var outbox = new OutboxMessageEntity
        {
            MessageId = messageId,
            Exchange = exchange,
            RoutingKey = reoutingKey,
            MessageHeader = System.Text.Json.JsonSerializer.Serialize(messageHeader, _transactionOptions.JsonSerializerOptions),
            MessageBody = Convert.ToBase64String(body),
            MessageText = _transactionOptions.Publisher.DisplayMessageText
                ? System.Text.Json.JsonSerializer.Serialize(message, _transactionOptions.JsonSerializerOptions)
                : "{}",
            RetryCount = 0,
            Status = (int)MessageStatus.Pending,
            NextRetryTime = now,
            CreateTime = now,
            UpdateTime = now
        };

        if (_hasConnection)
        {
            using var command = _dbConnection!.CreateCommand();
            command.Transaction = _dbTransaction;
            await _databaseProvider.InsertOutboxAsync(command, outbox, cancellationToken);
            return;
        }

        using var connection = _transactionOptions.Connection(_serviceProvider);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);

        using (var command = connection.CreateCommand())
        {
            await _databaseProvider.InsertOutboxAsync(command, outbox, cancellationToken);
        }

        scope.Complete();
    }
}
