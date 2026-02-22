// <copyright file="ITransactionOutboxPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction;

/// <summary>
/// Publisher for an outbox message that has already been registered in database.
/// </summary>
public interface ITransactionOutboxPublisher
{
    /// <summary>
    /// Gets message id.
    /// </summary>
    long MessageId { get; }

    /// <summary>
    /// Gets exchange.
    /// </summary>
    string Exchange { get; }

    /// <summary>
    /// Gets routing key.
    /// </summary>
    string RoutingKey { get; }

    /// <summary>
    /// Gets create time.
    /// </summary>
    DateTimeOffset CreateTime { get; }

    /// <summary>
    /// Publishes the registered message and updates outbox status when possible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(CancellationToken cancellationToken = default);
}
