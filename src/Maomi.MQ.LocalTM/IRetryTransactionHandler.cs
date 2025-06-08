// <copyright file="IRetryTransactionHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;

namespace Maomi.MQ.Transaction;

public interface IRetryTransactionHandler
{
    /// <summary>
    /// Failure to send trigger event.
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    /// <param name="entity"></param>
    /// <param name="exception"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task HandleSendFailedAsync(string exchange, string routingKey, PublisherEntity entity, Exception exception, CancellationToken cancellationToken = default);
}
