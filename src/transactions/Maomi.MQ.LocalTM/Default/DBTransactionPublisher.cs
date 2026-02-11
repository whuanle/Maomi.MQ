// <copyright file="DBTransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Data.Common;
using System.Transactions;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// While in the same transaction as the business database, when publishing messages, they will not be sent directly to MQ, but will first be stored in the database.
/// </summary>
public class DBTransactionPublisher : DefaultMessagePublisher, IDBTransactionPublisher
{
    private readonly bool _hasConnection;
    private readonly DbConnection _dbConnection;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IMQTransactionOptions _transactionOptions;

    private readonly DbTransaction? _dbTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="DBTransactionPublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    /// <param name="dbConnection"></param>
    /// <param name="dbTransaction"></param>
    internal DBTransactionPublisher(DefaultMessagePublisher publisher, DbConnection dbConnection, DbTransaction dbTransaction)
        : base(publisher)
    {
        _hasConnection = true;
        _transactionOptions = _serviceProvider.GetRequiredService<IMQTransactionOptions>();
        _dbConnection = dbConnection;
        _dbTransaction = dbTransaction;
        _databaseProvider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DBTransactionPublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    internal DBTransactionPublisher(DefaultMessagePublisher publisher)
        : base(publisher)
    {
        _transactionOptions = _serviceProvider.GetRequiredService<IMQTransactionOptions>();
        _dbConnection = _transactionOptions.Connection(_serviceProvider);
        _databaseProvider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
    }

    /// <inheritdoc/>
    public override async Task PublishChannelAsync<TMessage>(IChannel channel, string exchange, string reoutingKey, TMessage message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        var messageSerializer = _messageSerializer;
        if (messageSerializer is IMessageSerializerFactory serializerFactory)
        {
            messageSerializer = serializerFactory.GetMessageSerializer(typeof(TMessage));
        }

        var messageId = _idGen.NextId();

        MessageHeader messageHeader = new MessageHeader
        {
            Id = messageId.ToString(),
            Timestamp = DateTimeOffset.Now,
            AppId = _mqOptions.AppName,
            ContentEncoding = messageSerializer.ContentEncoding,
            ContentType = messageSerializer.ContentType,
            Type = typeof(TMessage).FullName!,
            UserId = properties.UserId ?? string.Empty,
            Exchange = exchange,
            RoutingKey = reoutingKey,
            Properties = properties
        };

        InitializeMessageProperties<TMessage>(properties, ref messageHeader);
        var body = _messageSerializer.Serializer(message);

        var entity = new PublisherEntity()
        {
            message_id = messageId,
            exchange = exchange,
            routing_key = reoutingKey,
            message_text = _transactionOptions.Publisher.DisplayMessageText ? System.Text.Json.JsonSerializer.Serialize(message, _transactionOptions.JsonSerializerOptions) : "{}",
            message_body = Convert.ToBase64String(body),
            retry_count = 0,
            status = (int)MessageStatus.None,
            properties = System.Text.Json.JsonSerializer.Serialize(messageHeader, _transactionOptions.JsonSerializerOptions),
            create_time = DateTimeOffset.Now,
            update_time = DateTimeOffset.Now,
        };

        if (_hasConnection == false)
        {
            using var tran = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.Serializable,
                },
                asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);
            {
                var command = _dbConnection.CreateCommand();
                await _databaseProvider.InsertUnSentMessage(command, entity);
                tran.Complete();
            }
        }
        else
        {
            var command = _dbConnection.CreateCommand();
            command.Transaction = _dbTransaction!;
            await _databaseProvider.InsertUnSentMessage(command, entity);
        }
    }
}