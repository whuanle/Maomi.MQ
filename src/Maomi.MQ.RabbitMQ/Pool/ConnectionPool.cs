// <copyright file="ConnectionPool.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.ObjectPool;

namespace Maomi.MQ.Pool;

/// <summary>
/// Object pool.<br />
/// 连接对象池.
/// </summary>
public class ConnectionPool : ObjectPool<ConnectionObject>
{
    private readonly DefaultObjectPoolProvider _defaultObjectPoolProvider;
    private readonly ConnectionPooledObjectPolicy _connectionPooledObjectPolicy;
    private readonly ObjectPool<ConnectionObject> _objectPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPool"/> class.
    /// </summary>
    /// <param name="connectionPooledObjectPolicy"></param>
    public ConnectionPool(ConnectionPooledObjectPolicy connectionPooledObjectPolicy)
    {
        _connectionPooledObjectPolicy = connectionPooledObjectPolicy;
        _defaultObjectPoolProvider = new DefaultObjectPoolProvider();

        _objectPool = _defaultObjectPoolProvider.Create<ConnectionObject>(_connectionPooledObjectPolicy);
    }

    /// <inheritdoc/>
    public override ConnectionObject Get()
    {
        return _objectPool.Get();
    }

    /// <inheritdoc/>
    public override void Return(ConnectionObject obj)
    {
        _objectPool.Return(obj);
    }
}
