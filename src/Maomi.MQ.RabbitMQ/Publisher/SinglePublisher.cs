// <copyright file="SinglePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1401
#pragma warning disable SA1600

using Maomi.MQ.Pool;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
/// </summary>
public class SinglePublisher : DefaultMessagePublisher, IMessagePublisher, IDisposable
{
    protected readonly ConnectionObject _connectionObject;
    protected readonly bool _isExchange;
    protected bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SinglePublisher"/> class.
    /// </summary>
    /// <param name="connectionObject"></param>
    /// <param name="publisher"></param>
    /// <param name="isExchange"></param>
    internal SinglePublisher(ConnectionObject connectionObject, DefaultMessagePublisher publisher, bool isExchange = false)
        : base(publisher)
    {
        _connectionObject = connectionObject;
        _isExchange = isExchange;
        _isExchange = isExchange;
    }

    /// <inheritdoc cref="IMessagePublisher.CustomPublishAsync{TEvent}(string, EventBody{TEvent}, BasicProperties)"/>
    public override Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        return PublishAsync(_connectionObject.Channel, queue, message, properties, _isExchange);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _connectionPool.Return(_connectionObject);
            }

            disposedValue = true;
        }
    }
}