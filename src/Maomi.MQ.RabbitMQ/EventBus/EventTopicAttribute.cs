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
public class EventTopicAttribute : Attribute, IConsumerOptions
{
    /// <inheritdoc />
    public string Queue { get; set; }

    /// <inheritdoc />
    public string? DeadQueue { get; set; }

    private ushort _qos = 10;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool RetryFaildRequeue { get; set; }

    /// <inheritdoc />
    public bool ExecptionRequeue { get; set; } = true;

    /// <inheritdoc />
    public int Expiration { get; set; }

    /// <inheritdoc />
    public string? Group { get; set; }

    /// <inheritdoc />
    public AutoQueueDeclare AutoQueueDeclare { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTopicAttribute"/> class.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    public EventTopicAttribute(string queue)
    {
        Queue = queue;
    }
}
