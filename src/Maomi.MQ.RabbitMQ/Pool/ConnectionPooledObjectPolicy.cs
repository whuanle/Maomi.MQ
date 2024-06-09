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
    private readonly MqOptions _mqOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPooledObjectPolicy"/> class.
    /// </summary>
    /// <param name="mqOptions"></param>
    public ConnectionPooledObjectPolicy(MqOptions mqOptions)
    {
        _mqOptions = mqOptions;
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
