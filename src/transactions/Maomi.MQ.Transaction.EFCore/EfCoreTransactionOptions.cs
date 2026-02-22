// <copyright file="EfCoreTransactionOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.EFCore;

/// <summary>
/// Options for EF Core transaction integration.
/// </summary>
public sealed class EfCoreTransactionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
    /// is called automatically after business delegate execution.
    /// </summary>
    public bool AutoSaveChanges { get; set; } = true;
}
