// <copyright file="ModelBuilderExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.EntityFrameworkCore;

namespace Maomi.MQ.Transaction.EFCore;

/// <summary>
/// Extensions for mapping Maomi.MQ transaction tables in EF Core model.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies entity mappings for transaction outbox and inbox barrier tables.
    /// </summary>
    /// <param name="modelBuilder">Model builder.</param>
    /// <param name="transactionOptions">Transaction options containing table names.</param>
    /// <returns>The model builder.</returns>
    public static ModelBuilder ApplyMaomiMQTransactionModel(
        this ModelBuilder modelBuilder,
        IMQTransactionOptions transactionOptions)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(transactionOptions);

        var outboxTable = string.IsNullOrWhiteSpace(transactionOptions.Publisher.TableName)
            ? "mq_publisher"
            : transactionOptions.Publisher.TableName;
        var inboxTable = string.IsNullOrWhiteSpace(transactionOptions.Consumer.TableName)
            ? "mq_consumer"
            : transactionOptions.Consumer.TableName;

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable(outboxTable);
            entity.HasKey(x => x.MessageId);

            entity.Property(x => x.MessageId).HasColumnName("message_id");
            entity.Property(x => x.Exchange).HasColumnName("exchange").HasMaxLength(256).IsRequired();
            entity.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(256).IsRequired();
            entity.Property(x => x.MessageHeader).HasColumnName("message_header").IsRequired();
            entity.Property(x => x.MessageBody).HasColumnName("message_body").IsRequired();
            entity.Property(x => x.MessageText).HasColumnName("message_text").IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").IsRequired();
            entity.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired();
            entity.Property(x => x.NextRetryTime).HasColumnName("next_retry_time").IsRequired();
            entity.Property(x => x.LockId).HasColumnName("lock_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.LockTime).HasColumnName("lock_time");
            entity.Property(x => x.LastError).HasColumnName("last_error").IsRequired();
            entity.Property(x => x.CreateTime).HasColumnName("create_time").IsRequired();
            entity.Property(x => x.UpdateTime).HasColumnName("update_time").IsRequired();

            entity.HasIndex(x => new { x.Status, x.NextRetryTime }).HasDatabaseName("ix_outbox_status_next_retry");
            entity.HasIndex(x => x.LockTime).HasDatabaseName("ix_outbox_lock_time");
        });

        modelBuilder.Entity<InboxBarrierEntity>(entity =>
        {
            entity.ToTable(inboxTable);
            entity.HasKey(x => new { x.ConsumerName, x.MessageId });

            entity.Property(x => x.ConsumerName).HasColumnName("consumer_name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.MessageId).HasColumnName("message_id");
            entity.Property(x => x.MessageHeader).HasColumnName("message_header").IsRequired();
            entity.Property(x => x.Exchange).HasColumnName("exchange").HasMaxLength(256).IsRequired();
            entity.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").IsRequired();
            entity.Property(x => x.LockId).HasColumnName("lock_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.LockTime).HasColumnName("lock_time");
            entity.Property(x => x.LastError).HasColumnName("last_error").IsRequired();
            entity.Property(x => x.CreateTime).HasColumnName("create_time").IsRequired();
            entity.Property(x => x.UpdateTime).HasColumnName("update_time").IsRequired();

            entity.HasIndex(x => new { x.Status, x.LockTime }).HasDatabaseName("ix_inbox_status_lock_time");
        });

        return modelBuilder;
    }
}
