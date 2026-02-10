// <copyright file="IQueueNameOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// 队列名称设置.
/// </summary>
public interface IQueueNameOptions
{
    /// <summary>
    /// Queue name or routing key name.<br />
    /// 队列名称或路由键名称.
    /// </summary>
    string RoutingKey { get; }

    /// <summary>
    /// Exchange.<br />
    /// 绑定交换器.
    /// </summary>
    public string? Exchange { get; }
}
