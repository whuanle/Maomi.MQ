// <copyright file="MaomiMQTransactionDbContext.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.EntityFrameworkCore;

namespace Maomi.MQ.Transaction.EFCore;

/// <summary>
/// Unified EF Core context for transaction outbox and inbox-barrier message tables.
/// </summary>
public class MaomiMQTransactionDbContext : DbContext, ITransactionMessageDbContext
{
    private readonly IMQTransactionOptions _transactionOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaomiMQTransactionDbContext"/> class.
    /// </summary>
    /// <param name="options">EF Core options.</param>
    /// <param name="transactionOptions">Transaction options.</param>
    public MaomiMQTransactionDbContext(
        DbContextOptions<MaomiMQTransactionDbContext> options,
        IMQTransactionOptions transactionOptions)
        : base(options)
    {
        _transactionOptions = transactionOptions;
    }

    /// <summary>
    /// Gets outbox messages table.
    /// </summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => this.Set<OutboxMessageEntity>();

    /// <summary>
    /// Gets inbox barrier table.
    /// </summary>
    public DbSet<InboxBarrierEntity> InboxBarriers => this.Set<InboxBarrierEntity>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyMaomiMQTransactionModel(_transactionOptions);
        base.OnModelCreating(modelBuilder);
    }
}
