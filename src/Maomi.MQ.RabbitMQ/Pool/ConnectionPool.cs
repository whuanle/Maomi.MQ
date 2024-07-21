// <copyright file="ConnectionPool.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Pool;

/// <summary>
/// Object pool.<br />
/// 连接对象池.
/// </summary>
public class ConnectionPool
{
    private readonly MqOptions _mqOptions;
    private readonly ConnectionObject _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPool"/> class.
    /// </summary>
    /// <param name="mqOptions"></param>
    public ConnectionPool(MqOptions mqOptions)
    {
        _mqOptions = mqOptions;
        _connection = Create();
    }

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns><see cref="ConnectionObject"/>.</returns>
    public ConnectionObject Get()
    {
        return _connection;
    }

    /// <summary>
    /// Create <see cref="ConnectionObject"/>.
    /// </summary>
    /// <returns><see cref="ConnectionObject"/>.</returns>
    public ConnectionObject Create()
    {
        return new ConnectionObject(_mqOptions);
    }
}