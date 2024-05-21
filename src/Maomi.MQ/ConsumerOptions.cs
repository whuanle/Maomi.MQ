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
public class ConsumerOptions
{
    /// <summary>
    /// Queue name.<br />
    /// 队列名称.
    /// </summary>
    public string Queue { get; init; } = null!;

    /// <summary>
    /// Whether to return to the queue when the number of consumption failures reaches the condition.<br />
    /// 消费失败次数达到条件时，是否放回队列.
    /// </summary>
    public bool RetryFaildRequeue { get; init; }

    /// <summary>
    /// Whether to put back to the queue when an exception occurs, such as a serialization error, rather than an exception occurred during consumption.<br />
    /// 出现异常时是否放回队列，例如序列化错误等原因导致的，而不是消费时发生异常导致的.
    /// </summary>
    public bool ExecptionRequeue { get; init; }

    /// <summary>
    /// Qos.
    /// </summary>
    public ushort Qos { get; init; }
}
