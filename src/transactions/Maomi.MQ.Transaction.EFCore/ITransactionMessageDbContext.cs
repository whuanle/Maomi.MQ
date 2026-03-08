// <copyright file="ITransactionMessageDbContext.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Maomi.MQ.Transaction.EFCore;

/// <summary>
/// Abstraction for a message storage DbContext containing outbox and inbox-barrier tables.
/// </summary>
public interface ITransactionMessageDbContext
{
    /// <summary>
    /// Gets outbox messages table.
    /// </summary>
    DbSet<OutboxMessageEntity> OutboxMessages { get; }

    /// <summary>
    /// Gets inbox barrier table.
    /// </summary>
    DbSet<InboxBarrierEntity> InboxBarriers { get; }

    /// <summary>
    /// Gets EF Core database facade.
    /// </summary>
    DatabaseFacade Database { get; }
}
