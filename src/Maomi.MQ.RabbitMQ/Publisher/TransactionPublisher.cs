// <copyright file="TransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// RabbitMQ transaction.
/// </summary>
public sealed class TransactionPublisher : SingleChannelPublisher, ITransactionPublisher, ISingleChannelPublisher, IMessagePublisher, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPublisher"/> class.
    /// </summary>
    /// <param name="messagePublisher"></param>
    internal TransactionPublisher(DefaultMessagePublisher messagePublisher)
        : base(messagePublisher)
    {
    }

    /// <inheritdoc />
    public Task TxSelectAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Value.TxSelectAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task TxCommitAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Value.TxCommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task TxRollbackAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Value.TxRollbackAsync(cancellationToken);
    }
}
