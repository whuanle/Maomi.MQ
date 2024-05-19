// <copyright file="ConnectionObject.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using RabbitMQ.Client;

namespace Maomi.MQ.Pool;

/// <summary>
/// IConnection,IChannel pool.<br />
/// TCP 连接和通道.
/// </summary>
public class ConnectionObject : IDisposable
{
    private readonly DefaultMqOptions _connectionOptions;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    /// <summary>
    /// IChannel.
    /// </summary>
    public IChannel Channel => _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionObject"/> class.
    /// </summary>
    /// <param name="connectionOptions"></param>
    public ConnectionObject(DefaultMqOptions connectionOptions)
    {
        _connectionOptions = connectionOptions;
        _connection = connectionOptions.ConnectionFactory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
