// <copyright file="IRetryTransactionHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Handles outbox retry failures.
/// </summary>
public interface IRetryTransactionHandler
{
    /// <summary>
    /// Invoked when sending outbox message failed.
    /// </summary>
    /// <param name="exchange">Exchange.</param>
    /// <param name="routingKey">Routing key.</param>
    /// <param name="entity">Outbox entity.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleSendFailedAsync(
        string exchange,
        string routingKey,
        OutboxMessageEntity entity,
        Exception exception,
        CancellationToken cancellationToken = default);
}
