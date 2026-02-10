// <copyright file="QueueNameAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Queue name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class QueueNameAttribute : Attribute, IQueueNameOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueueNameAttribute"/> class.
    /// </summary>
    /// <param name="routingKey"></param>
    public QueueNameAttribute(string routingKey)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(routingKey, nameof(routingKey));
        RoutingKey = routingKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueNameAttribute"/> class.
    /// </summary>
    /// <param name="exchange"></param>
    /// <param name="routingKey"></param>
    public QueueNameAttribute(string exchange, string routingKey)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
    }

    /// <summary>
    /// Exchange.<br />
    /// 绑定交换器.
    /// </summary>
    public string? Exchange { get; }

    /// <summary>
    /// Queue name or routing key name.<br />
    /// 队列名称或路由键名称.
    /// </summary>
    public string RoutingKey { get; }
}
