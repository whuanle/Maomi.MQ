// <copyright file="TransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// RabbitMQ transaction.
/// </summary>
public sealed class TransactionPublisher : SinglePublisher, IMessagePublisher, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionPublisher"/> class.
    /// </summary>
    /// <param name="connectionObject"></param>
    /// <param name="messagePublisher"></param>
    internal TransactionPublisher(ConnectionObject connectionObject, DefaultMessagePublisher messagePublisher)
        : base(connectionObject, messagePublisher)
    {
    }

    /// <inheritdoc cref="IChannel.TxSelectAsync(CancellationToken)"/>
    public Task TxSelectAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.TxSelectAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.TxCommitAsync(CancellationToken)"/>
    public Task TxCommitAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.TxCommitAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.TxRollbackAsync(CancellationToken)"/>
    public Task TxRollbackAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.TxRollbackAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _connectionObject.Dispose();
    }
}
