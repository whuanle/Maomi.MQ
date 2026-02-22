// <copyright file="SqlServerTransactionDatabaseProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Globalization;

namespace Maomi.MQ.Transaction.SqlServer;

/// <summary>
/// SQL Server transaction database provider.
/// </summary>
public sealed class SqlServerTransactionDatabaseProvider : IDatabaseProvider, IDatabaseProviderNamed
{
    private readonly string _outboxTable;
    private readonly string _inboxTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerTransactionDatabaseProvider"/> class.
    /// </summary>
    /// <param name="options">Transaction options.</param>
    public SqlServerTransactionDatabaseProvider(IMQTransactionOptions options)
    {
        _outboxTable = Quote(options.Publisher.TableName);
        _inboxTable = Quote(options.Consumer.TableName);
    }

    /// <inheritdoc/>
    public string ProviderName => TransactionProviderNames.SqlServer;

    /// <inheritdoc/>
    public async Task EnsureTablesExistAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            command,
            $"""
            IF OBJECT_ID(N'{_outboxTable}', N'U') IS NULL
            BEGIN
                CREATE TABLE {_outboxTable} (
                  [message_id] bigint NOT NULL,
                  [exchange] nvarchar(256) NOT NULL,
                  [routing_key] nvarchar(256) NOT NULL,
                  [message_header] nvarchar(max) NOT NULL,
                  [message_body] nvarchar(max) NOT NULL,
                  [message_text] nvarchar(max) NOT NULL,
                  [status] int NOT NULL,
                  [retry_count] int NOT NULL,
                  [next_retry_time] datetimeoffset(7) NOT NULL,
                  [lock_id] nvarchar(128) NOT NULL,
                  [lock_time] datetimeoffset(7) NULL,
                  [last_error] nvarchar(max) NOT NULL,
                  [create_time] datetimeoffset(7) NOT NULL,
                  [update_time] datetimeoffset(7) NOT NULL,
                  CONSTRAINT [PK_{CleanName(_outboxTable)}] PRIMARY KEY CLUSTERED ([message_id])
                );
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{CleanName(_outboxTable)}_status_next_retry' AND object_id = OBJECT_ID(N'{_outboxTable}'))
            BEGIN
                CREATE INDEX [IX_{CleanName(_outboxTable)}_status_next_retry] ON {_outboxTable}([status], [next_retry_time]);
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{CleanName(_outboxTable)}_lock_time' AND object_id = OBJECT_ID(N'{_outboxTable}'))
            BEGIN
                CREATE INDEX [IX_{CleanName(_outboxTable)}_lock_time] ON {_outboxTable}([lock_time]);
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"""
            IF OBJECT_ID(N'{_inboxTable}', N'U') IS NULL
            BEGIN
                CREATE TABLE {_inboxTable} (
                  [consumer_name] nvarchar(200) NOT NULL,
                  [message_id] bigint NOT NULL,
                  [message_header] nvarchar(max) NOT NULL,
                  [exchange] nvarchar(256) NOT NULL,
                  [routing_key] nvarchar(256) NOT NULL,
                  [status] int NOT NULL,
                  [lock_id] nvarchar(128) NOT NULL,
                  [lock_time] datetimeoffset(7) NULL,
                  [last_error] nvarchar(max) NOT NULL,
                  [create_time] datetimeoffset(7) NOT NULL,
                  [update_time] datetimeoffset(7) NOT NULL,
                  CONSTRAINT [PK_{CleanName(_inboxTable)}] PRIMARY KEY CLUSTERED ([consumer_name], [message_id])
                );
            END;
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{CleanName(_inboxTable)}_status_lock_time' AND object_id = OBJECT_ID(N'{_inboxTable}'))
            BEGIN
                CREATE INDEX [IX_{CleanName(_inboxTable)}_status_lock_time] ON {_inboxTable}([status], [lock_time]);
            END;
            """,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task InsertOutboxAsync(DbCommand command, OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            INSERT INTO {_outboxTable}
            ([message_id],[exchange],[routing_key],[message_header],[message_body],[message_text],[status],[retry_count],[next_retry_time],[lock_id],[lock_time],[last_error],[create_time],[update_time])
            VALUES
            (@message_id,@exchange,@routing_key,@message_header,@message_body,@message_text,@status,@retry_count,@next_retry_time,@lock_id,@lock_time,@last_error,@create_time,@update_time);
            """;

        AddParameter(command, "@message_id", message.MessageId);
        AddParameter(command, "@exchange", message.Exchange);
        AddParameter(command, "@routing_key", message.RoutingKey);
        AddParameter(command, "@message_header", message.MessageHeader);
        AddParameter(command, "@message_body", message.MessageBody);
        AddParameter(command, "@message_text", message.MessageText ?? "{}");
        AddParameter(command, "@status", message.Status);
        AddParameter(command, "@retry_count", message.RetryCount);
        AddParameter(command, "@next_retry_time", message.NextRetryTime);
        AddParameter(command, "@lock_id", message.LockId ?? string.Empty);
        AddParameter(command, "@lock_time", message.LockTime.HasValue ? message.LockTime.Value : DBNull.Value);
        AddParameter(command, "@last_error", message.LastError ?? string.Empty);
        AddParameter(command, "@create_time", message.CreateTime);
        AddParameter(command, "@update_time", message.UpdateTime);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TryLockOutboxAsync(
        DbCommand command,
        long messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_outboxTable}
            SET
                [status] = @processing,
                [lock_id] = @lock_id,
                [lock_time] = @now,
                [update_time] = @now
            WHERE
                [message_id] = @message_id
                AND [status] IN (@pending, @failed)
                AND ([lock_id] = N'' OR [lock_id] IS NULL);
            """;

        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@pending", (int)MessageStatus.Pending);
        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@lock_id", lockId);
        AddParameter(command, "@now", now);
        AddParameter(command, "@message_id", messageId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<int> TryLockOutboxBatchAsync(
        DbCommand command,
        string lockId,
        DateTimeOffset now,
        TimeSpan lockTimeout,
        int maxRetry,
        int take,
        CancellationToken cancellationToken = default)
    {
        var expireTime = now.Add(-lockTimeout);

        command.Parameters.Clear();
        command.CommandText =
            $"""
            ;WITH cte AS (
                SELECT TOP (@take) [message_id]
                FROM {_outboxTable} WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE
                    [retry_count] < @max_retry
                    AND [next_retry_time] <= @now
                    AND (
                        [status] IN (@pending, @failed)
                        OR ([status] = @processing AND ([lock_time] IS NULL OR [lock_time] < @expire_time))
                    )
                ORDER BY [create_time] ASC
            )
            UPDATE o
            SET
                [status] = @processing,
                [lock_id] = @lock_id,
                [lock_time] = @now,
                [update_time] = @now
            FROM {_outboxTable} AS o
            INNER JOIN cte ON o.[message_id] = cte.[message_id];
            """;

        AddParameter(command, "@take", take);
        AddParameter(command, "@max_retry", maxRetry);
        AddParameter(command, "@now", now);
        AddParameter(command, "@pending", (int)MessageStatus.Pending);
        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@expire_time", expireTime);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessageEntity>> GetLockedOutboxBatchAsync(
        DbCommand command,
        string lockId,
        int take,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            SELECT TOP (@take)
                [message_id],[exchange],[routing_key],[message_header],[message_body],[message_text],[status],[retry_count],[next_retry_time],[lock_id],[lock_time],[last_error],[create_time],[update_time]
            FROM {_outboxTable}
            WHERE [lock_id] = @lock_id AND [status] = @processing
            ORDER BY [update_time] ASC;
            """;

        AddParameter(command, "@take", take);
        AddParameter(command, "@lock_id", lockId);
        AddParameter(command, "@processing", (int)MessageStatus.Processing);

        List<OutboxMessageEntity> rows = new();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(ReadOutbox(reader));
        }

        return rows;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkOutboxSucceededAsync(
        DbCommand command,
        long messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_outboxTable}
            SET
                [status] = @succeeded,
                [lock_id] = N'',
                [lock_time] = NULL,
                [last_error] = N'',
                [update_time] = @now
            WHERE [message_id] = @message_id AND [lock_id] = @lock_id;
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@now", now);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkOutboxFailedAsync(
        DbCommand command,
        long messageId,
        string lockId,
        DateTimeOffset now,
        DateTimeOffset nextRetryTime,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_outboxTable}
            SET
                [status] = @failed,
                [retry_count] = [retry_count] + 1,
                [next_retry_time] = @next_retry_time,
                [last_error] = @last_error,
                [lock_id] = N'',
                [lock_time] = NULL,
                [update_time] = @now
            WHERE [message_id] = @message_id AND [lock_id] = @lock_id;
            """;

        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@next_retry_time", nextRetryTime);
        AddParameter(command, "@last_error", errorMessage ?? string.Empty);
        AddParameter(command, "@now", now);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<InboxBarrierEnterResult> TryEnterInboxBarrierAsync(
        DbCommand command,
        InboxBarrierEntity barrier,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        try
        {
            command.Parameters.Clear();
            command.CommandText =
                $"""
                INSERT INTO {_inboxTable}
                ([consumer_name],[message_id],[message_header],[exchange],[routing_key],[status],[lock_id],[lock_time],[last_error],[create_time],[update_time])
                VALUES
                (@consumer_name,@message_id,@message_header,@exchange,@routing_key,@status,@lock_id,@lock_time,@last_error,@create_time,@update_time);
                """;

            AddParameter(command, "@consumer_name", barrier.ConsumerName);
            AddParameter(command, "@message_id", barrier.MessageId);
            AddParameter(command, "@message_header", barrier.MessageHeader);
            AddParameter(command, "@exchange", barrier.Exchange);
            AddParameter(command, "@routing_key", barrier.RoutingKey);
            AddParameter(command, "@status", (int)MessageStatus.Processing);
            AddParameter(command, "@lock_id", barrier.LockId);
            AddParameter(command, "@lock_time", barrier.LockTime ?? DateTimeOffset.UtcNow);
            AddParameter(command, "@last_error", barrier.LastError ?? string.Empty);
            AddParameter(command, "@create_time", barrier.CreateTime);
            AddParameter(command, "@update_time", barrier.UpdateTime);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return InboxBarrierEnterResult.Entered;
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            return await TryUpdateInboxBarrierAsync(command, barrier, lockTimeout, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MarkInboxBarrierSucceededAsync(
        DbCommand command,
        string consumerName,
        long messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_inboxTable}
            SET
                [status] = @succeeded,
                [lock_id] = N'',
                [lock_time] = NULL,
                [last_error] = N'',
                [update_time] = @now
            WHERE [consumer_name] = @consumer_name AND [message_id] = @message_id AND [lock_id] = @lock_id;
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@now", now);
        AddParameter(command, "@consumer_name", consumerName);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkInboxBarrierFailedAsync(
        DbCommand command,
        string consumerName,
        long messageId,
        string lockId,
        DateTimeOffset now,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_inboxTable}
            SET
                [status] = @failed,
                [lock_id] = N'',
                [lock_time] = NULL,
                [last_error] = @last_error,
                [update_time] = @now
            WHERE [consumer_name] = @consumer_name AND [message_id] = @message_id AND [lock_id] = @lock_id;
            """;

        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@last_error", errorMessage ?? string.Empty);
        AddParameter(command, "@now", now);
        AddParameter(command, "@consumer_name", consumerName);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<long> CountSucceededOutboxAsync(
        DbCommand command,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText = $"SELECT COUNT(1) FROM {_outboxTable} WHERE [status] = @succeeded;";
        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public async Task<long> CountSucceededInboxAsync(
        DbCommand command,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText = $"SELECT COUNT(1) FROM {_inboxTable} WHERE [status] = @succeeded;";
        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteSucceededOutboxBeforeAsync(
        DbCommand command,
        DateTimeOffset cutoffTime,
        int take,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            ;WITH picked AS (
                SELECT TOP (@take) [message_id]
                FROM {_outboxTable}
                WHERE [status] = @succeeded AND [update_time] < @cutoff_time
                ORDER BY [update_time] ASC
            )
            DELETE o
            FROM {_outboxTable} AS o
            INNER JOIN picked ON o.[message_id] = picked.[message_id];
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@cutoff_time", cutoffTime);
        AddParameter(command, "@take", take);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteSucceededInboxBeforeAsync(
        DbCommand command,
        DateTimeOffset cutoffTime,
        int take,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            ;WITH picked AS (
                SELECT TOP (@take) [consumer_name], [message_id]
                FROM {_inboxTable}
                WHERE [status] = @succeeded AND [update_time] < @cutoff_time
                ORDER BY [update_time] ASC
            )
            DELETE i
            FROM {_inboxTable} AS i
            INNER JOIN picked ON i.[consumer_name] = picked.[consumer_name] AND i.[message_id] = picked.[message_id];
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@cutoff_time", cutoffTime);
        AddParameter(command, "@take", take);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOldestSucceededOutboxAsync(
        DbCommand command,
        int take,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            ;WITH picked AS (
                SELECT TOP (@take) [message_id]
                FROM {_outboxTable}
                WHERE [status] = @succeeded
                ORDER BY [update_time] ASC
            )
            DELETE o
            FROM {_outboxTable} AS o
            INNER JOIN picked ON o.[message_id] = picked.[message_id];
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@take", take);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteOldestSucceededInboxAsync(
        DbCommand command,
        int take,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            ;WITH picked AS (
                SELECT TOP (@take) [consumer_name], [message_id]
                FROM {_inboxTable}
                WHERE [status] = @succeeded
                ORDER BY [update_time] ASC
            )
            DELETE i
            FROM {_inboxTable} AS i
            INNER JOIN picked ON i.[consumer_name] = picked.[consumer_name] AND i.[message_id] = picked.[message_id];
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@take", take);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<InboxBarrierEnterResult> TryUpdateInboxBarrierAsync(
        DbCommand command,
        InboxBarrierEntity barrier,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken)
    {
        var current = await GetInboxStatusAsync(command, barrier.ConsumerName, barrier.MessageId, cancellationToken);
        if (current == null)
        {
            return InboxBarrierEnterResult.Busy;
        }

        if (current.Value.Status == (int)MessageStatus.Succeeded)
        {
            return InboxBarrierEnterResult.AlreadyCompleted;
        }

        var expireTime = (barrier.LockTime ?? DateTimeOffset.UtcNow).Add(-lockTimeout);

        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_inboxTable}
            SET
                [status] = @processing,
                [lock_id] = @lock_id,
                [lock_time] = @lock_time,
                [message_header] = @message_header,
                [exchange] = @exchange,
                [routing_key] = @routing_key,
                [update_time] = @update_time
            WHERE
                [consumer_name] = @consumer_name
                AND [message_id] = @message_id
                AND [status] <> @succeeded
                AND ([status] <> @processing OR [lock_time] IS NULL OR [lock_time] < @expire_time);
            """;

        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@lock_id", barrier.LockId);
        AddParameter(command, "@lock_time", barrier.LockTime ?? DateTimeOffset.UtcNow);
        AddParameter(command, "@message_header", barrier.MessageHeader);
        AddParameter(command, "@exchange", barrier.Exchange);
        AddParameter(command, "@routing_key", barrier.RoutingKey);
        AddParameter(command, "@update_time", barrier.UpdateTime);
        AddParameter(command, "@consumer_name", barrier.ConsumerName);
        AddParameter(command, "@message_id", barrier.MessageId);
        AddParameter(command, "@expire_time", expireTime);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows > 0)
        {
            return InboxBarrierEnterResult.Entered;
        }

        current = await GetInboxStatusAsync(command, barrier.ConsumerName, barrier.MessageId, cancellationToken);
        if (current != null && current.Value.Status == (int)MessageStatus.Succeeded)
        {
            return InboxBarrierEnterResult.AlreadyCompleted;
        }

        return InboxBarrierEnterResult.Busy;
    }

    private async Task<(int Status, DateTimeOffset? LockTime)?> GetInboxStatusAsync(
        DbCommand command,
        string consumerName,
        long messageId,
        CancellationToken cancellationToken)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            SELECT [status], [lock_time]
            FROM {_inboxTable} WITH (UPDLOCK, ROWLOCK)
            WHERE [consumer_name] = @consumer_name AND [message_id] = @message_id;
            """;

        AddParameter(command, "@consumer_name", consumerName);
        AddParameter(command, "@message_id", messageId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var status = Convert.ToInt32(reader["status"], CultureInfo.InvariantCulture);
        DateTimeOffset? lockTime = null;
        if (!reader.IsDBNull(reader.GetOrdinal("lock_time")))
        {
            lockTime = ReadDateTimeOffset(reader["lock_time"]);
        }

        return (status, lockTime);
    }

    private static OutboxMessageEntity ReadOutbox(DbDataReader reader)
    {
        return new OutboxMessageEntity
        {
            MessageId = Convert.ToInt64(reader["message_id"], CultureInfo.InvariantCulture),
            Exchange = reader["exchange"].ToString() ?? string.Empty,
            RoutingKey = reader["routing_key"].ToString() ?? string.Empty,
            MessageHeader = reader["message_header"].ToString() ?? string.Empty,
            MessageBody = reader["message_body"].ToString() ?? string.Empty,
            MessageText = reader["message_text"].ToString() ?? string.Empty,
            Status = Convert.ToInt32(reader["status"], CultureInfo.InvariantCulture),
            RetryCount = Convert.ToInt32(reader["retry_count"], CultureInfo.InvariantCulture),
            NextRetryTime = ReadDateTimeOffset(reader["next_retry_time"]),
            LockId = reader["lock_id"].ToString() ?? string.Empty,
            LockTime = reader["lock_time"] is DBNull ? null : ReadDateTimeOffset(reader["lock_time"]),
            LastError = reader["last_error"].ToString() ?? string.Empty,
            CreateTime = ReadDateTimeOffset(reader["create_time"]),
            UpdateTime = ReadDateTimeOffset(reader["update_time"])
        };
    }

    private static DateTimeOffset ReadDateTimeOffset(object value)
    {
        if (value is DateTimeOffset offset)
        {
            return offset;
        }

        if (value is DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            return new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero);
        }

        return DateTimeOffset.Parse(value.ToString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task ExecuteAsync(DbCommand command, string sql, CancellationToken cancellationToken)
    {
        command.Parameters.Clear();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string Quote(string name)
    {
        return $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    private static string CleanName(string name)
    {
        return name.Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace('.', '_');
    }
}
