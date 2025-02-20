// <copyright file="ConnectionObject.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1401
#pragma warning disable SA1600

using RabbitMQ.Client;

namespace Maomi.MQ.Pool;

/// <summary>
/// IConnection,IChannel pool.<br />
/// TCP 连接和通道.
/// </summary>
public class ConnectionObject : IDisposeConnectionObject
{
    protected Lazy<IConnection> _connection;
    protected Lazy<IChannel> _channel;
    private bool disposedValue;

    /// <summary>
    /// IConnection.
    /// </summary>
    public IConnection Connection => _connection.Value;

    /// <summary>
    /// IChannel.
    /// </summary>
    public IChannel DefaultChannel => _channel.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionObject"/> class.
    /// </summary>
    /// <param name="mqOptions"></param>
    public ConnectionObject(MqOptions mqOptions)
    {
        _connection = new Lazy<IConnection>(() => mqOptions.ConnectionFactory.CreateConnectionAsync().Result);
        _channel = new Lazy<IChannel>(() => _connection.Value.CreateChannelAsync().Result);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionObject"/> class.
    /// </summary>
    /// <param name="connectionObject"></param>
    protected ConnectionObject(ConnectionObject connectionObject)
    {
        _connection = connectionObject._connection;
        _channel = connectionObject._channel;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Connection.Dispose();
            }

            disposedValue = true;
        }
    }

    ~ConnectionObject()
    {
        Dispose(disposing: false);
    }
}