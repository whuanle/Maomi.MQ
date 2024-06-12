// <copyright file="PoolExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;

namespace Maomi.MQ;

/// <summary>
/// Pool extensions.
/// </summary>
public static class PoolExtensions
{
    /// <summary>
    /// Creates an object that automatically returns the connection pool.<br />
    /// 创建自动归还连接池的对象.
    /// </summary>
    /// <param name="connectionPool"></param>
    /// <returns><see cref="AutoReturnPoolObject"/>.</returns>
    public static AutoReturnPoolObject CreateAutoReturn(this ConnectionPool connectionPool)
    {
        var obj = connectionPool.Get();
        return new AutoReturnPoolObject(connectionPool, obj);
    }
}