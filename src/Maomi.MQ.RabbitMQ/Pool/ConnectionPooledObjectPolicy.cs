// <copyright file="ConnectionPooledObjectPolicy.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.ObjectPool;

namespace Maomi.MQ.Pool;

/// <summary>
/// Object pool policy.<br />
/// 对象池策略.
/// </summary>
public class ConnectionPooledObjectPolicy : PooledObjectPolicy<ConnectionObject>
{
    /// <summary>
    /// The maximum number of objects to retain in the pool.
    /// </summary>
    public int MaximumRetained { get; private set; } = Environment.ProcessorCount * 2;

    private readonly MqOptions _mqOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPooledObjectPolicy"/> class.
    /// </summary>
    /// <param name="mqOptions"></param>
    public ConnectionPooledObjectPolicy(MqOptions mqOptions)
    {
        _mqOptions = mqOptions;
        MaximumRetained = mqOptions.PoolMaximumRetained;
    }

    /// <inheritdoc/>
    public override ConnectionObject Create()
    {
        return new ConnectionObject(_mqOptions);
    }

    /// <inheritdoc/>
    public override bool Return(ConnectionObject obj)
    {
        return true;
    }
}
