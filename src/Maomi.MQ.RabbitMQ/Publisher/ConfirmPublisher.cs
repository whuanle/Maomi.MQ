// <copyright file="ConfirmPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
/// </summary>
public sealed class ConfirmPublisher : SinglePublisher, IMessagePublisher, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmPublisher"/> class.
    /// </summary>
    /// <param name="connectionObject"></param>
    /// <param name="publisher"></param>
    /// <param name="isExchange"></param>
    internal ConfirmPublisher(ConnectionObject connectionObject, DefaultMessagePublisher publisher, bool isExchange)
        : base(connectionObject, publisher, isExchange)
    {
    }

    /// <inheritdoc cref="IChannel.ConfirmSelectAsync(CancellationToken)"/>
    public Task ConfirmSelectAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.ConfirmSelectAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.WaitForConfirmsOrDieAsync(CancellationToken)"/>
    public Task WaitForConfirmsOrDieAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.WaitForConfirmsOrDieAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
    public Task<bool> WaitForConfirmsAsync(CancellationToken cancellationToken = default)
    {
        return _connectionObject.Channel.WaitForConfirmsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _connectionObject.Dispose();
    }
}