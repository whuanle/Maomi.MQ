// <copyright file="MQPublisherTransactionOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Models;

/// <summary>
/// Publisher transaction options.
/// </summary>
public class MQPublisherTransactionOptions
{
    /// <summary>
    /// Table name.
    /// </summary>
    public string TableName { get; init; } = default!;

    /// <summary>
    /// Maximum retry count for publisher.
    /// </summary>
    public int MaxRetry { get; init; } = default!;

    /// <summary>
    /// The message content will be recorded in the database. It is recommended to use this feature only in debug mode.
    /// </summary>
    public bool DisplayMessageText { get; init; } = default!;

    /// <summary>
    /// Retry interval factory for publisher.
    /// </summary>
    public RetryPolicyFactory RetryInterval { get; init; } = default!;

    /// <summary>
    /// Interval for scanning the database.
    /// </summary>
    public TimeSpan ScanDbInterval { get; init; } = default!;
}
