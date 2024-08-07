﻿// <copyright file="TransactionPublisher.cs" company="Maomi">
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
    /// <param name="messagePublisher"></param>
    /// <param name="isExchange"></param>
    internal TransactionPublisher(DefaultMessagePublisher messagePublisher, bool isExchange)
        : base(messagePublisher, isExchange)
    {
    }

    /// <inheritdoc cref="IChannel.TxSelectAsync(CancellationToken)"/>
    public Task TxSelectAsync(CancellationToken cancellationToken = default)
    {
        return _channel.TxSelectAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.TxCommitAsync(CancellationToken)"/>
    public Task TxCommitAsync(CancellationToken cancellationToken = default)
    {
        return _channel.TxCommitAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.TxRollbackAsync(CancellationToken)"/>
    public Task TxRollbackAsync(CancellationToken cancellationToken = default)
    {
        return _channel.TxRollbackAsync(cancellationToken);
    }

    /// <inheritdoc cref="IMessagePublisher.CustomPublishAsync{TEvent}(string, EventBody{TEvent}, BasicProperties)"/>
    public override Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        return PublishAsync(_channel, queue, message, properties, _isExchange);
    }
}
