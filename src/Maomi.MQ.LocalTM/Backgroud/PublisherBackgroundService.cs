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
/// Scan the message to be sent in the background and ensure that it is delivered correctly.
/// </summary>
public class PublisherBackgroundService : BackgroundService
{
    // Avoid connection interruption caused by prolonged inactivity.
    private const string PreferredTestQuery = "SELECT 1";

    private readonly IMQTransactionOptions _transactionTableOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IConsumerTypeProvider _consumerTypeProvider;
    private readonly IConnectionObject _connectionObject;
    private readonly ILogger<PublisherBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublisherBackgroundService"/> class.
    /// </summary>
    /// <param name="transactionTableOptions"></param>
    /// <param name="serviceScopeFactory"></param>
    /// <param name="databaseProvider"></param>
    /// <param name="consumerTypeProvider"></param>
    /// <param name="connectionPool"></param>
    /// <param name="logger"></param>
    public PublisherBackgroundService(
        IMQTransactionOptions transactionTableOptions,
        IServiceScopeFactory serviceScopeFactory,
        IDatabaseProvider databaseProvider,
        IConsumerTypeProvider consumerTypeProvider,
        ConnectionPool connectionPool,
        ILogger<PublisherBackgroundService> logger)
    {
        _transactionTableOptions = transactionTableOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _databaseProvider = databaseProvider;
        _consumerTypeProvider = consumerTypeProvider;
        _connectionObject = connectionPool.Get();
        _logger = logger;
    }

    private DbConnection _dbConnection = default!;
    private DateTimeOffset _lastDbConnectionCheckTime;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_transactionTableOptions.AutoCreateTable)
        {
            await CreateTableAsync();
        }

        while (true)
        {
            try
            {
                var status = await SendMessage(stoppingToken);
                if (status > 0)
                {
                    // 数据库可能还有未发送的消息，需要迅速处理.
                    // The database may still have unsent messages that need to be processed promptly.
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while reading from the database and sending a message");
            }

            await Task.Delay(_transactionTableOptions.Publisher.ScanDbInterval, stoppingToken);
        }
    }

    private async Task<int> SendMessage(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var ioc = scope.ServiceProvider;

        var dbConnection = await GetDbConnection(ioc);
        var messpublisher = ioc.GetRequiredService<IChannelMessagePublisher>();

        using var dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);
        using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;
        var entity = await _databaseProvider.GetUnSentMessage(command);

        if (entity == null)
        {
            return 0;
        }

        try
        {
            await RetryAndSendAsync(ioc, messpublisher, entity, cancellationToken);

            entity.status = (int)MessageStatus.Confirmed;
            entity.update_time = DateTimeOffset.Now;

            var insertCommand = dbConnection.CreateCommand();
            insertCommand.Transaction = dbTransaction;
            await _databaseProvider.UpdateUnSentMessage(insertCommand, entity);

            dbTransaction.Commit();

            return 1;
        }
        catch (Exception ex)
        {
            dbTransaction.Rollback();

            _logger.LogError(ex, "Failed to send message {MessageId} to exchange {Exchange} with routing key {RoutingKey}.", entity.message_id, entity.exchange, entity.routing_key);
            entity.status = (int)MessageStatus.Failed;
            entity.retry_count++;
            entity.update_time = DateTimeOffset.Now;

            var updateCommand = dbConnection.CreateCommand();
            await _databaseProvider.UpdateUnSentMessage(updateCommand, entity);

            var retryTracsactionHandler = ioc.GetService<IRetryTransactionHandler>();
            if (retryTracsactionHandler != null)
            {
                await retryTracsactionHandler.HandleSendFailedAsync(
                    entity.exchange,
                    entity.routing_key,
                    entity,
                    ex,
                    cancellationToken);
            }
        }

        if (entity.status == (int)MessageStatus.Failed && entity.retry_count >= _transactionTableOptions.Publisher.MaxRetry)
        {
            _logger.LogError("Message {MessageId} has reached the maximum retry count and will not be retried again.", entity.message_id);
        }

        return -1;
    }

    private async Task RetryAndSendAsync(IServiceProvider ioc, IChannelMessagePublisher messpublisher, PublisherEntity entity, CancellationToken cancellationToken)
    {
        var messageBody = Convert.FromBase64String(entity.message_body);
        var messageHeader = System.Text.Json.JsonSerializer.Deserialize<MessageHeader>(json: entity.properties, _transactionTableOptions.JsonSerializerOptions);
        BasicProperties properties = System.Text.Json.JsonSerializer.Deserialize<BasicProperties>(messageHeader.Properties!.ToString()!, _transactionTableOptions.JsonSerializerOptions)!;

        if (entity.retry_count > 1)
        {
            _logger.LogWarning("Retrying message {MessageId} for the {RetryCount} time.", entity.message_id, entity.retry_count);
        }

        var retryPolicy = _transactionTableOptions.Publisher.RetryInterval(ioc, entity.routing_key, messageHeader, entity.retry_count);

        if (retryPolicy.TotalSeconds > 0)
        {
            await Task.Delay(delay: retryPolicy, cancellationToken);
        }

        await messpublisher.PublishChannelAsync(
            _connectionObject.DefaultChannel,
            entity.exchange,
            entity.routing_key,
            messageBody,
            properties);
    }

    private async Task<DbConnection> GetDbConnection(IServiceProvider ioc)
    {
        if (_dbConnection == null)
        {
            _dbConnection = _transactionTableOptions.Connection.Invoke(ioc);
        }

        if (_dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }

        if ((DateTimeOffset.Now - _lastDbConnectionCheckTime) > TimeSpan.FromMinutes(30))
        {
            _lastDbConnectionCheckTime = DateTimeOffset.Now;
            using var cmd = _dbConnection.CreateCommand();
            cmd.CommandText = PreferredTestQuery;
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed, reopening connection.");
                _dbConnection.Close();
                _dbConnection.Dispose();
                _dbConnection = _transactionTableOptions.Connection.Invoke(ioc);
            }
        }

        return _dbConnection;
    }

    private async Task CreateTableAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var ioc = scope.ServiceProvider;

        var dbConnection = await GetDbConnection(ioc);
        var command = dbConnection.CreateCommand();

        try
        {
            await _databaseProvider.EnsureTablesExistAsync(command);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Failed to create transaction table.");
            throw;
        }
    }
}