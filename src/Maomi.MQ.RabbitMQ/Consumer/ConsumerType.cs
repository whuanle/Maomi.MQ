// <copyright file="ConsumerType.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Consumer;

/// <summary>
/// All consumers should be recorded under the "ConsumerType" category.<br />
/// 所有消费者都应该被记录到 ConsumerType.
/// </summary>
public class ConsumerType : IComparable<ConsumerType>
{
    /// <summary>
    /// Queue.
    /// </summary>
    public string Queue { get; init; } = null!;

    /// <summary>
    /// <see cref="IConsumer{TMessage}"/>.
    /// </summary>
    public Type Consumer { get; init; } = null!;

    /// <summary>
    /// Event model.
    /// </summary>
    public Type Event { get; init; } = null!;

    /// <summary>
    /// Consumer options.
    /// </summary>
    public IConsumerOptions ConsumerOptions { get; init; } = null!;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (ConsumerOptions.IsBroadcast)
        {
            return HashCode.Combine(Queue, Consumer.Name.ToLower());
        }

        return HashCode.Combine(Queue);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj is ConsumerType consumerType)
        {
            if (this.ConsumerOptions.IsBroadcast && consumerType.ConsumerOptions.IsBroadcast)
            {
                return consumerType.Queue == this.Queue && consumerType.Consumer == this.Consumer;
            }

            return consumerType.Queue == this.Queue;
        }

        return false;
    }

    /// <inheritdoc/>
    public int CompareTo(ConsumerType? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (other.ConsumerOptions.IsBroadcast && this.ConsumerOptions.IsBroadcast)
        {
            var queueComparison = this.Queue.CompareTo(other.Queue);
            if (queueComparison != 0)
            {
                return queueComparison;
            }

            return this.Consumer.Name.ToLower().CompareTo(other.Consumer.Name.ToLower());
        }

        return this.Queue.CompareTo(other?.Queue);
    }
}
