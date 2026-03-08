// <copyright file="OutboxMessageEntity.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Outbox message row.
/// </summary>
public sealed class OutboxMessageEntity
{
    /// <summary>
    /// Gets or sets message id.
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// Gets or sets exchange.
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets routing key.
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets serialized message header.
    /// </summary>
    public string MessageHeader { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets serialized message body text.
    /// </summary>
    public string MessageBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets debug message text.
    /// </summary>
    public string MessageText { get; set; } = "{}";

    /// <summary>
    /// Gets or sets message status.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets next retry time.
    /// </summary>
    public DateTimeOffset NextRetryTime { get; set; }

    /// <summary>
    /// Gets or sets process lock id.
    /// </summary>
    public string LockId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets process lock time.
    /// </summary>
    public DateTimeOffset? LockTime { get; set; }

    /// <summary>
    /// Gets or sets last error message.
    /// </summary>
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets create time.
    /// </summary>
    public DateTimeOffset CreateTime { get; set; }

    /// <summary>
    /// Gets or sets update time.
    /// </summary>
    public DateTimeOffset UpdateTime { get; set; }
}
