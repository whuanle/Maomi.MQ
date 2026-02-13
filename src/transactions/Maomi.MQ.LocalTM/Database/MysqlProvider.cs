// <copyright file="MysqlProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Models;
using System.Data.Common;

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Mysql database provider.
/// </summary>
public class MysqlProvider : IDatabaseProvider
{
    private readonly IMQTransactionOptions _transactionTableOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MysqlProvider"/> class.
    /// </summary>
    /// <param name="transactionTableOptions"></param>
    public MysqlProvider(IMQTransactionOptions transactionTableOptions)
    {
        _transactionTableOptions = transactionTableOptions;
    }

    /// <inheritdoc/>
    public async Task EnsureTablesExistAsync(DbCommand command)
    {
        var sql =
            $"""
            CREATE TABLE IF NOT EXISTS `{_transactionTableOptions.Publisher.TableName}` (
              `message_id` bigint(20) NOT NULL COMMENT 'message id',
              `exchange` varchar(100) NOT NULL DEFAULT '' COMMENT 'exchange',
              `routing_key` varchar(100) NOT NULL COMMENT 'ruoting key',
              `properties` text NOT NULL COMMENT 'MessageHeader object',
              `message_body` text NOT NULL DEFAULT '{"{}"}' COMMENT 'Binary message body, the specific content is related to the serializer',
              `message_text` text NOT NULL DEFAULT '' COMMENT 'Serializing the message body using JSON will increase the data row size. You can set `DisplayMessageText=false` to disable it.',
              `status` int(11) NOT NULL DEFAULT 0 COMMENT 'send satus',
              `retry_count` int(11) NOT NULL DEFAULT 0 COMMENT 'retry count',
              `create_time` datetime NOT NULL DEFAULT utc_timestamp() COMMENT 'create time',
              `update_time` datetime NOT NULL DEFAULT utc_timestamp() COMMENT 'update time',
              PRIMARY KEY (`message_id`)
            ) COMMENT='MaomiMQ';
            CREATE TABLE IF NOT EXISTS `{_transactionTableOptions.Consumer.TableName}` (
              `message_id` varchar(20) NOT NULL DEFAULT '0' COMMENT 'message id',
              `message_header` text NOT NULL DEFAULT '{"{}"}' COMMENT 'message header',
              `exchange` varchar(100) NOT NULL DEFAULT '' COMMENT 'exchange',
              `routing_key` varchar(100) NOT NULL COMMENT 'ruoting key',
              `status` int(11) NOT NULL DEFAULT 0 COMMENT 'send satus',
              `create_time` datetime NOT NULL DEFAULT utc_timestamp() COMMENT 'create time',
              PRIMARY KEY (`message_id`)
            ) COMMENT='MaomiMQ';
            """;

        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public async Task InsertReceivedMessage(DbCommand command, ConsumerEntity entity)
    {
        await command.InsertAsync(entity, _transactionTableOptions.Consumer.TableName);
    }

    /// <inheritdoc/>
    public async Task<PublisherEntity?> GetUnSentMessage(DbCommand command)
    {
        const string SQL = @"SELECT * FROM `{0}` WHERE `status` IN (0, 3) AND `retry_count` < 10 LIMIT 1 for update SKIP LOCKED;";

        var selectSQL = string.Format(SQL, _transactionTableOptions.Publisher.TableName);

        command.CommandText = selectSQL;
        return await command.QueryScalarAsync<PublisherEntity>();
    }

    /// <inheritdoc/>
    public async Task UpdateReceivedMessage(DbCommand command, ConsumerEntity entity)
    {
        await command.UpdateAsync<ConsumerEntity>(entity, _transactionTableOptions.Publisher.TableName, "message_id");
    }

    /// <inheritdoc/>
    public async Task UpdateUnSentMessage(DbCommand command, PublisherEntity entity)
    {
        await command.UpdateAsync<PublisherEntity>(entity, _transactionTableOptions.Consumer.TableName, "message_id");
    }

    /// <inheritdoc/>
    public async Task<ConsumerEntity?> GetReceivedMessage(DbCommand command, string messageId)
    {
        const string SQL = @"SELECT * FROM `{0}` WHERE `message_id` = @messageId LIMIT 1;";
        var selectSQL = string.Format(SQL, _transactionTableOptions.Consumer.TableName);

        command.CommandText = selectSQL;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@messageId";
        parameter.Value = messageId;

        command.Parameters.Add(parameter);

        return await command.QueryScalarAsync<ConsumerEntity>();
    }

    /// <inheritdoc/>
    public async Task InsertUnSentMessage(DbCommand command, PublisherEntity publisher)
    {
        await command.InsertAsync(publisher, _transactionTableOptions.Publisher.TableName);
    }
}
