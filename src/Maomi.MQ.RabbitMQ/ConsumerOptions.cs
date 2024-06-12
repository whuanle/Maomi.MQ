// <copyright file="ConsumerOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer options.<br />
/// 消费者配置.
/// </summary>
public class ConsumerOptions : IConsumerOptions
{
    /// <inheritdoc />
    public string Queue { get; set; } = null!;

    /// <inheritdoc />
    public string? DeadQueue { get; set; }

    /// <inheritdoc />
    public ushort Qos { get; set; }

    /// <inheritdoc />
    public bool RetryFaildRequeue { get; set; }

    /// <inheritdoc />
    public bool ExecptionRequeue { get; set; }

    /// <inheritdoc />
    public int Expiration { get; set; }

    /// <inheritdoc />
    public string? Group { get; set; }

    /// <inheritdoc />
    public AutoQueueDeclare AutoQueueDeclare { get; set; }
}
