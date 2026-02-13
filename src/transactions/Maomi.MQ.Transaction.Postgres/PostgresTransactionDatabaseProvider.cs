// <copyright file="PostgresTransactionDatabaseProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Npgsql;
using System.Data.Common;
using System.Globalization;

namespace Maomi.MQ.Transaction.Postgres;

/// <summary>
/// PostgreSQL transaction database provider.
/// </summary>
public sealed class PostgresTransactionDatabaseProvider : IDatabaseProvider, IDatabaseProviderNamed
{
    private readonly string _outboxTable;
    private readonly string _inboxTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTransactionDatabaseProvider"/> class.
    /// </summary>
    /// <param name="options">Transaction options.</param>
    public PostgresTransactionDatabaseProvider(IMQTransactionOptions options)
    {
        _outboxTable = Quote(options.Publisher.TableName);
        _inboxTable = Quote(options.Consumer.TableName);
    }

    /// <inheritdoc/>
    public string ProviderName => TransactionProviderNames.Postgres;

    /// <inheritdoc/>
    public async Task EnsureTablesExistAsync(DbCommand command, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            command,
            $"""
            CREATE TABLE IF NOT EXISTS {_outboxTable} (
              message_id varchar(64) NOT NULL,
              exchange varchar(256) NOT NULL,
              routing_key varchar(256) NOT NULL,
              message_header text NOT NULL,
              message_body text NOT NULL,
              message_text text NOT NULL,
              status int NOT NULL,
              retry_count int NOT NULL,
              next_retry_time timestamptz NOT NULL,
              lock_id varchar(128) NOT NULL,
              lock_time timestamptz NULL,
              last_error text NOT NULL,
              create_time timestamptz NOT NULL,
              update_time timestamptz NOT NULL,
              PRIMARY KEY (message_id)
            );
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"CREATE INDEX IF NOT EXISTS ix_outbox_status_next_retry ON {_outboxTable} (status, next_retry_time);",
            cancellationToken);

        await ExecuteAsync(
            command,
            $"CREATE INDEX IF NOT EXISTS ix_outbox_lock_time ON {_outboxTable} (lock_time);",
            cancellationToken);

        await ExecuteAsync(
            command,
            $"""
            CREATE TABLE IF NOT EXISTS {_inboxTable} (
              consumer_name varchar(200) NOT NULL,
              message_id varchar(64) NOT NULL,
              message_header text NOT NULL,
              exchange varchar(256) NOT NULL,
              routing_key varchar(256) NOT NULL,
              status int NOT NULL,
              lock_id varchar(128) NOT NULL,
              lock_time timestamptz NULL,
              last_error text NOT NULL,
              create_time timestamptz NOT NULL,
              update_time timestamptz NOT NULL,
              PRIMARY KEY (consumer_name, message_id)
            );
            """,
            cancellationToken);

        await ExecuteAsync(
            command,
            $"CREATE INDEX IF NOT EXISTS ix_inbox_status_lock_time ON {_inboxTable} (status, lock_time);",
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task InsertOutboxAsync(DbCommand command, OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            INSERT INTO {_outboxTable}
            (message_id,exchange,routing_key,message_header,message_body,message_text,status,retry_count,next_retry_time,lock_id,lock_time,last_error,create_time,update_time)
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
        AddParameter(command, "@next_retry_time", message.NextRetryTime.UtcDateTime);
        AddParameter(command, "@lock_id", message.LockId ?? string.Empty);
        AddParameter(command, "@lock_time", message.LockTime.HasValue ? message.LockTime.Value.UtcDateTime : DBNull.Value);
        AddParameter(command, "@last_error", message.LastError ?? string.Empty);
        AddParameter(command, "@create_time", message.CreateTime.UtcDateTime);
        AddParameter(command, "@update_time", message.UpdateTime.UtcDateTime);

        await command.ExecuteNonQueryAsync(cancellationToken);
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
            WITH cte AS (
                SELECT message_id
                FROM {_outboxTable}
                WHERE
                    retry_count < @max_retry
                    AND next_retry_time <= @now
                    AND (
                        status IN (@pending, @failed)
                        OR (status = @processing AND (lock_time IS NULL OR lock_time < @expire_time))
                    )
                ORDER BY create_time ASC
                LIMIT @take
                FOR UPDATE SKIP LOCKED
            )
            UPDATE {_outboxTable} AS o
            SET
                status = @processing,
                lock_id = @lock_id,
                lock_time = @now,
                update_time = @now
            FROM cte
            WHERE o.message_id = cte.message_id;
            """;

        AddParameter(command, "@max_retry", maxRetry);
        AddParameter(command, "@now", now.UtcDateTime);
        AddParameter(command, "@pending", (int)MessageStatus.Pending);
        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@expire_time", expireTime.UtcDateTime);
        AddParameter(command, "@take", take);
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
            SELECT
                message_id,exchange,routing_key,message_header,message_body,message_text,status,retry_count,next_retry_time,lock_id,lock_time,last_error,create_time,update_time
            FROM {_outboxTable}
            WHERE lock_id = @lock_id AND status = @processing
            ORDER BY update_time ASC
            LIMIT @take;
            """;

        AddParameter(command, "@lock_id", lockId);
        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@take", take);

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
        string messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_outboxTable}
            SET
                status = @succeeded,
                lock_id = '',
                lock_time = NULL,
                last_error = '',
                update_time = @now
            WHERE message_id = @message_id AND lock_id = @lock_id;
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@now", now.UtcDateTime);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkOutboxFailedAsync(
        DbCommand command,
        string messageId,
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
                status = @failed,
                retry_count = retry_count + 1,
                next_retry_time = @next_retry_time,
                last_error = @last_error,
                lock_id = '',
                lock_time = NULL,
                update_time = @now
            WHERE message_id = @message_id AND lock_id = @lock_id;
            """;

        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@next_retry_time", nextRetryTime.UtcDateTime);
        AddParameter(command, "@last_error", errorMessage ?? string.Empty);
        AddParameter(command, "@now", now.UtcDateTime);
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
                (consumer_name,message_id,message_header,exchange,routing_key,status,lock_id,lock_time,last_error,create_time,update_time)
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
            AddParameter(command, "@lock_time", (barrier.LockTime ?? DateTimeOffset.UtcNow).UtcDateTime);
            AddParameter(command, "@last_error", barrier.LastError ?? string.Empty);
            AddParameter(command, "@create_time", barrier.CreateTime.UtcDateTime);
            AddParameter(command, "@update_time", barrier.UpdateTime.UtcDateTime);

            await command.ExecuteNonQueryAsync(cancellationToken);
            return InboxBarrierEnterResult.Entered;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return await TryUpdateInboxBarrierAsync(command, barrier, lockTimeout, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MarkInboxBarrierSucceededAsync(
        DbCommand command,
        string consumerName,
        string messageId,
        string lockId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            UPDATE {_inboxTable}
            SET
                status = @succeeded,
                lock_id = '',
                lock_time = NULL,
                last_error = '',
                update_time = @now
            WHERE consumer_name = @consumer_name AND message_id = @message_id AND lock_id = @lock_id;
            """;

        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@now", now.UtcDateTime);
        AddParameter(command, "@consumer_name", consumerName);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> MarkInboxBarrierFailedAsync(
        DbCommand command,
        string consumerName,
        string messageId,
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
                status = @failed,
                lock_id = '',
                lock_time = NULL,
                last_error = @last_error,
                update_time = @now
            WHERE consumer_name = @consumer_name AND message_id = @message_id AND lock_id = @lock_id;
            """;

        AddParameter(command, "@failed", (int)MessageStatus.Failed);
        AddParameter(command, "@last_error", errorMessage ?? string.Empty);
        AddParameter(command, "@now", now.UtcDateTime);
        AddParameter(command, "@consumer_name", consumerName);
        AddParameter(command, "@message_id", messageId);
        AddParameter(command, "@lock_id", lockId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
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
                status = @processing,
                lock_id = @lock_id,
                lock_time = @lock_time,
                message_header = @message_header,
                exchange = @exchange,
                routing_key = @routing_key,
                update_time = @update_time
            WHERE
                consumer_name = @consumer_name
                AND message_id = @message_id
                AND status <> @succeeded
                AND (status <> @processing OR lock_time IS NULL OR lock_time < @expire_time);
            """;

        AddParameter(command, "@processing", (int)MessageStatus.Processing);
        AddParameter(command, "@succeeded", (int)MessageStatus.Succeeded);
        AddParameter(command, "@lock_id", barrier.LockId);
        AddParameter(command, "@lock_time", (barrier.LockTime ?? DateTimeOffset.UtcNow).UtcDateTime);
        AddParameter(command, "@message_header", barrier.MessageHeader);
        AddParameter(command, "@exchange", barrier.Exchange);
        AddParameter(command, "@routing_key", barrier.RoutingKey);
        AddParameter(command, "@update_time", barrier.UpdateTime.UtcDateTime);
        AddParameter(command, "@consumer_name", barrier.ConsumerName);
        AddParameter(command, "@message_id", barrier.MessageId);
        AddParameter(command, "@expire_time", expireTime.UtcDateTime);

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
        string messageId,
        CancellationToken cancellationToken)
    {
        command.Parameters.Clear();
        command.CommandText =
            $"""
            SELECT status, lock_time
            FROM {_inboxTable}
            WHERE consumer_name = @consumer_name AND message_id = @message_id
            LIMIT 1;
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
            MessageId = reader["message_id"].ToString() ?? string.Empty,
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
        return $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
