// <copyright file="ConsumerAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer.<br />
/// 消费者配置.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ConsumerAttribute : Attribute, IConsumerOptions
{
    /// <inheritdoc />
    public string Queue { get; set; }

    /// <inheritdoc />
    public string? DeadQueue { get; set; }

    /// <inheritdoc />
    public ushort Qos { get; set; } = 100;

    /// <inheritdoc />
    public bool RetryFaildRequeue { get; set; }

    /// <inheritdoc />
    public bool ExecptionRequeue { get; set; } = true;

    /// <inheritdoc />
    public int Expiration { get; set; }

    /// <inheritdoc />
    public AutoQueueDeclare AutoQueueDeclare { get; set; }

    /// <inheritdoc />
    public string? BindExchange { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerAttribute"/> class.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    public ConsumerAttribute(string queue)
    {
        Queue = queue;
    }
}
