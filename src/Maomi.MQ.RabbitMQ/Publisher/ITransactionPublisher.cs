// <copyright file="TransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// RabbitMQ transaction.
/// </summary>
public interface ITransactionPublisher : ISingleChannelPublisher, IMessagePublisher, IDisposable
{
    /// <inheritdoc cref="IChannel.TxSelectAsync(CancellationToken)"/>
    Task TxSelectAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IChannel.TxCommitAsync(CancellationToken)"/>
    Task TxCommitAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="IChannel.TxRollbackAsync(CancellationToken)"/>
    Task TxRollbackAsync(CancellationToken cancellationToken = default);
}
