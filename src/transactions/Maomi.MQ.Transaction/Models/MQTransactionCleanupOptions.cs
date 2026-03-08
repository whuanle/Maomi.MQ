// <copyright file="MQTransactionCleanupOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Models;

/// <summary>
/// Cleanup options for transaction message tables.
/// </summary>
public class MQTransactionCleanupOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether cleanup is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets cleanup scan interval.
    /// </summary>
    public TimeSpan ScanInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the days to keep completed rows.
    /// When null or less than or equal to zero, this condition is disabled.
    /// </summary>
    public int? KeepCompletedDays { get; init; }

    /// <summary>
    /// Gets or sets maximum completed rows kept per table.
    /// When null or less than or equal to zero, this condition is disabled.
    /// </summary>
    public long? MaxCompletedCount { get; init; }

    /// <summary>
    /// Gets or sets max delete rows per SQL statement.
    /// </summary>
    public int DeleteBatchSize { get; init; } = 500;
}
