// <copyright file="IIdProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Snowflake id generator.<br />
/// 雪花 id 生成器.
/// </summary>
public interface IIdProvider
{
    /// <summary>
    /// Get snowflake id.<br />
    /// 获取雪花 id.
    /// </summary>
    /// <returns>id.</returns>
    long NextId();
}
