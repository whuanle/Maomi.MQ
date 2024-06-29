// <copyright file="MqOptionsBuilder.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Build options.
/// </summary>
public class MqOptionsBuilder
{
    /// <summary>
    /// App name.
    /// </summary>
    public string AppName { get; set; } = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "unknown";

    /// <summary>
    /// 0-64.
    /// </summary>
    public int WorkId { get; set; }

    /// <summary>
    /// Create queues on startup,<see cref="RabbitMQ.Client.IChannel.QueueDeclareAsync"/>.<br />
    /// 是否自动创建队列.
    /// </summary>
    public bool AutoQueueDeclare { get; set; } = true;

    /// <summary>
    /// The maximum number of objects to retain in the pool.<br />
    /// 常驻连接池的最大数量.
    /// </summary>
    public int PoolMaximumRetained { get; init; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// RabbitMQ connection factory.
    /// </summary>
    public Action<ConnectionFactory> Rabbit { get; set; } = null!;
}