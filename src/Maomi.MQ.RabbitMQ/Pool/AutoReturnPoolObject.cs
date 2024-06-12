// <copyright file="AutoReturnPoolObject.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Pool;

/// <summary>
/// Auto return connection pool.
/// </summary>
public sealed class AutoReturnPoolObject : ConnectionObject, IDisposable
{
    private readonly ConnectionPool _connectionPool;
    private readonly ConnectionObject _connectionObject;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoReturnPoolObject"/> class.
    /// </summary>
    /// <param name="connectionPool"></param>
    /// <param name="connectionObject"></param>
    public AutoReturnPoolObject(ConnectionPool connectionPool, ConnectionObject connectionObject)
        : base(connectionObject)
    {
        _connectionPool = connectionPool;
        _connectionObject = connectionObject;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
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