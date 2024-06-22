// <copyright file="ConfirmPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
/// </summary>
public sealed class ConfirmPublisher : SinglePublisher, IMessagePublisher, IDisposable
{
    private readonly IChannel _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmPublisher"/> class.
    /// </summary>
    /// <param name="connectionObject"></param>
    /// <param name="publisher"></param>
    /// <param name="isExchange"></param>
    internal ConfirmPublisher(ConnectionObject connectionObject, DefaultMessagePublisher publisher, bool isExchange)
        : base(connectionObject, publisher, isExchange)
    {
        _channel = connectionObject.Connection.CreateChannelAsync().Result;
    }

    /// <inheritdoc cref="IChannel.ConfirmSelectAsync(CancellationToken)"/>
    public Task ConfirmSelectAsync(CancellationToken cancellationToken = default)
    {
        return _channel.ConfirmSelectAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.WaitForConfirmsOrDieAsync(CancellationToken)"/>
    public Task WaitForConfirmsOrDieAsync(CancellationToken cancellationToken = default)
    {
        return _channel.WaitForConfirmsOrDieAsync(cancellationToken);
    }

    /// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
    public Task<bool> WaitForConfirmsAsync(CancellationToken cancellationToken = default)
    {
        return _channel.WaitForConfirmsAsync(cancellationToken);
    }

    /// <inheritdoc cref="IMessagePublisher.CustomPublishAsync{TEvent}(string, EventBody{TEvent}, BasicProperties)"/>
    public override Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        return PublishAsync(_channel, queue, message, properties, _isExchange);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _channel.Dispose();
                _connectionPool.Return(_connectionObject);
            }

            disposedValue = true;
        }
    }
}