// <copyright file="EventTopicAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// 事件主题.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EventTopicAttribute : Attribute
{
    /// <summary>
    /// Queue name.<br />
    /// 队列名称.
    /// </summary>
    public string Queue { get; set; } = null!;

    /// <summary>
    /// Group.<br />
    /// 分组名称.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Whether to return to the queue when the number of consumption failures reaches the condition.<br />
    /// 消费失败次数达到条件时，是否放回队列.
    /// </summary>
    public bool RetryFaildRequeue { get; internal set; }

    /// <summary>
    /// Whether to put back to the queue when an exception occurs, such as a serialization error, rather than an exception occurred during consumption.
    /// 出现异常时是否放回队列，例如序列化错误等原因导致的，而不是消费时发生异常导致的.
    /// </summary>
    public bool ExecptionRequeue { get; internal set; }

    private ushort _qos = 10;

    /// <summary>
    /// Qos.
    /// </summary>
    public ushort Qos
    {
        get => _qos;
        set
        {
            if (value <= 0)
            {
                _qos = 1;
            }
            else
            {
                _qos = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTopicAttribute"/> class.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    public EventTopicAttribute(string queue)
    {
        Queue = queue;
    }
}
