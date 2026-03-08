// <copyright file="TransactionOutboxService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Attributes;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Data;
using System.Data.Common;
using System.Globalization;

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Default implementation of <see cref="ITransactionOutboxService"/>.
/// </summary>
public sealed class TransactionOutboxService : ITransactionOutboxService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdProvider _idProvider;
    private readonly MqOptions _mqOptions;
    private readonly IRoutingProvider _routingProvider;
    private readonly ITransactionMessageSerializer _serializerSelector;
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IDatabaseProvider _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionOutboxService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <param name="idProvider">Id provider.</param>
    /// <param name="mqOptions">MQ options.</param>
    /// <param name="routingProvider">Routing provider.</param>
    /// <param name="serializerSelector">Transaction serializer selector.</param>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="databaseProvider">Database provider.</param>
    public TransactionOutboxService(
        IServiceProvider serviceProvider,
        IIdProvider idProvider,
        MqOptions mqOptions,
        IRoutingProvider routingProvider,
        ITransactionMessageSerializer serializerSelector,
        IMQTransactionOptions transactionOptions,
        IDatabaseProvider databaseProvider)
    {
        _serviceProvider = serviceProvider;
        _idProvider = idProvider;
        _mqOptions = mqOptions;
        _routingProvider = routingProvider;
        _serializerSelector = serializerSelector;
        _transactionOptions = transactionOptions;
        _databaseProvider = databaseProvider;
    }

    /// <inheritdoc/>
    public Task<ITransactionOutboxPublisher> RegisterAutoAsync<TMessage>(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        TMessage message,
        Action<BasicProperties>? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var queue = ResolveQueueRoute(message.GetType());
        var basicProperties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };
        properties?.Invoke(basicProperties);

        return RegisterAsync(
            dbConnection,
            dbTransaction,
            queue.Exchange ?? string.Empty,
            queue.RoutingKey,
            message,
            basicProperties,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ITransactionOutboxPublisher> RegisterAsync<TMessage>(
        DbConnection dbConnection,
        DbTransaction dbTransaction,
        string exchange,
        string routingKey,
        TMessage message,
        BasicProperties? properties = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateConnectionAndTransaction(dbConnection, dbTransaction);

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        properties ??= new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        var serializer = _serializerSelector.GetSerializer(message);
        var messageId = ResolveMessageId(properties);
        var messageIdText = MessageIdConverter.ToText(messageId);
        var now = DateTimeOffset.UtcNow;
        var messageType = message.GetType().FullName ?? typeof(TMessage).FullName ?? typeof(TMessage).Name;

        var messageHeader = new MessageHeader
        {
            Id = messageIdText,
            Timestamp = now,
            AppId = _mqOptions.AppName,
            ContentType = serializer.ContentType,
            Type = messageType,
            Exchange = exchange,
            RoutingKey = routingKey,
            Properties = properties
        };

        InitializeMessageProperties(properties, messageHeader, serializer, messageType);
        var body = serializer.Serializer(message);

        var outbox = new OutboxMessageEntity
        {
            MessageId = messageId,
            Exchange = exchange,
            RoutingKey = routingKey,
            MessageHeader = TransactionMessageStorageSerializer.SerializeHeader(messageHeader, _transactionOptions.JsonSerializerOptions),
            MessageBody = TransactionMessageStorageSerializer.SerializeBody(body),
            MessageText = _transactionOptions.Publisher.DisplayMessageText
                ? System.Text.Json.JsonSerializer.Serialize(message, _transactionOptions.JsonSerializerOptions)
                : "{}",
            RetryCount = 0,
            Status = (int)MessageStatus.Pending,
            NextRetryTime = now,
            LockId = string.Empty,
            LockTime = null,
            LastError = string.Empty,
            CreateTime = now,
            UpdateTime = now
        };

        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        await _databaseProvider.InsertOutboxAsync(command, outbox, cancellationToken);

        return ActivatorUtilities.CreateInstance<TransactionOutboxPublisher>(
            _serviceProvider,
            outbox,
            messageHeader,
            body,
            properties);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsSucceededAsync(long messageId, CancellationToken cancellationToken = default)
    {
        using var dbConnection = _transactionOptions.Connection(_serviceProvider);
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        using var dbTransaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        var lockId = string.Empty;
        var updated = await _databaseProvider.MarkOutboxSucceededAsync(
            command,
            messageId,
            lockId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (updated)
        {
            dbTransaction.Commit();
        }
        else
        {
            dbTransaction.Rollback();
        }

        return updated;
    }

    private IRouterKeyOptions ResolveQueueRoute(Type messageType)
    {
        var routerKey = messageType
            .GetCustomAttributes(typeof(RouterKeyAttribute), true)
            .OfType<IRouterKeyOptions>()
            .FirstOrDefault();

        if (routerKey == null)
        {
            throw new InvalidOperationException($"The message type [{messageType.FullName}] does not have the [{nameof(RouterKeyAttribute)}] attribute.");
        }

        return _routingProvider.Get(routerKey);
    }

    private static void InitializeMessageProperties(
        BasicProperties properties,
        MessageHeader messageHeader,
        IMessageSerializer serializer,
        string messageType)
    {
        properties.AppId = messageHeader.AppId;
        properties.ContentType = serializer.ContentType;
        properties.MessageId = messageHeader.Id;
        properties.Timestamp = new AmqpTimestamp(messageHeader.Timestamp.ToUnixTimeMilliseconds());
        properties.Type = messageType;
        properties.Headers ??= new Dictionary<string, object?>();
    }

    private long ResolveMessageId(BasicProperties properties)
    {
        if (!string.IsNullOrWhiteSpace(properties.MessageId))
        {
            return MessageIdConverter.ParseRequired(properties.MessageId, "BasicProperties.MessageId");
        }

        var generated = _idProvider.NextId();
        properties.MessageId = generated.ToString(CultureInfo.InvariantCulture);
        return generated;
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
