// <copyright file="PublisherBackgroundService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Data;
using System.Data.Common;

namespace Maomi.MQ.Transaction.Backgroud;

/// <summary>
/// Outbox dispatcher background service.
/// </summary>
public class PublisherBackgroundService : BackgroundService
{
    private readonly IMQTransactionOptions _transactionOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConnectionPool _connectionPool;
    private readonly ILogger<PublisherBackgroundService> _logger;

    private DbConnection? _dbConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublisherBackgroundService"/> class.
    /// </summary>
    /// <param name="transactionOptions">Transaction options.</param>
    /// <param name="serviceScopeFactory">Scope factory.</param>
    /// <param name="connectionPool">RabbitMQ connection pool.</param>
    /// <param name="logger">Logger.</param>
    public PublisherBackgroundService(
        IMQTransactionOptions transactionOptions,
        IServiceScopeFactory serviceScopeFactory,
        ConnectionPool connectionPool,
        ILogger<PublisherBackgroundService> logger)
    {
        _transactionOptions = transactionOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _connectionPool = connectionPool;
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
        if (_transactionOptions.AutoCreateTable)
        {
            await EnsureTableAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var handled = await ProcessOnceAsync(stoppingToken);
                if (handled > 0)
                {
                    continue;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher loop failed.");
            }

            await Task.Delay(_transactionOptions.Publisher.ScanDbInterval, stoppingToken);
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

    private async Task<int> ProcessOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
        var dbConnection = await GetDbConnectionAsync(serviceProvider, cancellationToken);

        int lockCount;
        using (var transaction = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            using var lockCommand = dbConnection.CreateCommand();
            lockCommand.Transaction = transaction;

            lockCount = await databaseProvider.TryLockOutboxBatchAsync(
                lockCommand,
                _transactionOptions.NodeId,
                DateTimeOffset.UtcNow,
                _transactionOptions.Publisher.LockTimeout,
                _transactionOptions.Publisher.MaxRetry,
                _transactionOptions.Publisher.MaxFetchCountPerLoop,
                cancellationToken);

            transaction.Commit();
        }

        if (lockCount <= 0)
        {
            return 0;
        }

        IReadOnlyList<OutboxMessageEntity> messages;
        using (var queryCommand = dbConnection.CreateCommand())
        {
            messages = await databaseProvider.GetLockedOutboxBatchAsync(
                queryCommand,
                _transactionOptions.NodeId,
                _transactionOptions.Publisher.MaxFetchCountPerLoop,
                cancellationToken);
        }

        if (messages.Count == 0)
        {
            return 0;
        }

        var publisher = serviceProvider.GetRequiredService<IChannelMessagePublisher>();
        var channel = _connectionPool.Get().DefaultChannel;

        int handled = 0;
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var success = await DispatchOneAsync(serviceProvider, databaseProvider, dbConnection, channel, publisher, message, cancellationToken);
            if (success)
            {
                handled++;
            }
        }

        return handled;
    }

    private async Task<bool> DispatchOneAsync(
        IServiceProvider serviceProvider,
        IDatabaseProvider databaseProvider,
        DbConnection dbConnection,
        IChannel channel,
        IChannelMessagePublisher publisher,
        OutboxMessageEntity message,
        CancellationToken cancellationToken)
    {
        try
        {
            var header = System.Text.Json.JsonSerializer.Deserialize<MessageHeader>(message.MessageHeader, _transactionOptions.JsonSerializerOptions);
            if (string.IsNullOrWhiteSpace(header.Id))
            {
                header = new MessageHeader
                {
                    Id = message.MessageId,
                    Exchange = message.Exchange,
                    RoutingKey = message.RoutingKey,
                    Timestamp = DateTimeOffset.UtcNow,
                    Properties = new BasicProperties
                    {
                        DeliveryMode = DeliveryModes.Persistent
                    }
                };
            }

            var properties = System.Text.Json.JsonSerializer.Deserialize<BasicProperties>(header.Properties?.ToString() ?? "{}", _transactionOptions.JsonSerializerOptions)
                ?? new BasicProperties { DeliveryMode = DeliveryModes.Persistent };

            var bytes = Convert.FromBase64String(message.MessageBody);

            if (message.RetryCount > 0)
            {
                var delay = _transactionOptions.Publisher.RetryInterval(serviceProvider, message.RoutingKey, header, message.RetryCount);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            await publisher.PublishChannelAsync(
                channel,
                message.Exchange,
                message.RoutingKey,
                header,
                bytes,
                properties,
                cancellationToken);

            using var tx = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
            using var command = dbConnection.CreateCommand();
            command.Transaction = tx;

            var updated = await databaseProvider.MarkOutboxSucceededAsync(
                command,
                message.MessageId,
                _transactionOptions.NodeId,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (!updated)
            {
                tx.Rollback();
                _logger.LogWarning("Outbox success status not updated for message {MessageId}.", message.MessageId);
                return false;
            }

            tx.Commit();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dispatch outbox message failed, messageId {MessageId}, exchange {Exchange}, routingKey {RoutingKey}.", message.MessageId, message.Exchange, message.RoutingKey);

            try
            {
                using var tx = dbConnection.BeginTransaction(IsolationLevel.ReadCommitted);
                using var command = dbConnection.CreateCommand();
                command.Transaction = tx;

                var header = System.Text.Json.JsonSerializer.Deserialize<MessageHeader>(message.MessageHeader, _transactionOptions.JsonSerializerOptions);
                if (string.IsNullOrWhiteSpace(header.Id))
                {
                    header = new MessageHeader
                    {
                        Id = message.MessageId,
                        Exchange = message.Exchange,
                        RoutingKey = message.RoutingKey,
                        Timestamp = DateTimeOffset.UtcNow,
                        Properties = new BasicProperties
                        {
                            DeliveryMode = DeliveryModes.Persistent
                        }
                    };
                }

                var delay = _transactionOptions.Publisher.RetryInterval(
                    serviceProvider,
                    message.RoutingKey,
                    header,
                    message.RetryCount + 1);

                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                var nextRetryTime = DateTimeOffset.UtcNow.Add(delay);
                var error = Truncate(ex.ToString(), _transactionOptions.Publisher.MaxErrorLength);
                var updated = await databaseProvider.MarkOutboxFailedAsync(
                    command,
                    message.MessageId,
                    _transactionOptions.NodeId,
                    DateTimeOffset.UtcNow,
                    nextRetryTime,
                    error,
                    cancellationToken);

                if (updated)
                {
                    tx.Commit();
                }
                else
                {
                    tx.Rollback();
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Update outbox failed state failed, messageId {MessageId}.", message.MessageId);
            }

            var retryHandler = serviceProvider.GetService<IRetryTransactionHandler>();
            if (retryHandler != null)
            {
                await retryHandler.HandleSendFailedAsync(
                    message.Exchange,
                    message.RoutingKey,
                    message,
                    ex,
                    cancellationToken);
            }

            return false;
        }
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
        var dbConnection = await GetDbConnectionAsync(serviceProvider, cancellationToken);

        using var command = dbConnection.CreateCommand();
        await databaseProvider.EnsureTablesExistAsync(command, cancellationToken);
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
