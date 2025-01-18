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
    public string? DeadExchange { get; set; }

    /// <inheritdoc />
    public string? DeadRoutingKey { get; set; }

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

    /// <inheritdoc />
    public string? ExchangeType { get; set; }

    /// <inheritdoc />
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerAttribute"/> class.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    public ConsumerAttribute(string queue)
    {
        ArgumentException.ThrowIfNullOrEmpty(queue, nameof(queue));
        Queue = queue;
    }

    /// <inheritdoc />
    public IConsumerOptions Clone()
    {
        var newOptions = new ConsumerAttribute(string.Empty);
        newOptions.CopyFrom(this);
        return newOptions;
    }

    /// <inheritdoc />
    public void CopyFrom(IConsumerOptions options)
    {
        this.Queue = options.Queue;
        this.DeadExchange = options.DeadExchange;
        this.DeadRoutingKey = options.DeadRoutingKey;
        this.Qos = options.Qos;
        this.RetryFaildRequeue = options.RetryFaildRequeue;
        this.ExecptionRequeue = options.ExecptionRequeue;
        this.Expiration = options.Expiration;
        this.AutoQueueDeclare = options.AutoQueueDeclare;
        this.BindExchange = options.BindExchange;
        this.ExchangeType = options.ExchangeType;
        this.RoutingKey = options.RoutingKey;
    }
}
