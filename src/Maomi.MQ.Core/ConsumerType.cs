// <copyright file="ConsumerType.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer info.
/// </summary>
public class ConsumerType : IComparable<ConsumerType>
{
    /// <summary>
    /// Queue.
    /// </summary>
    public string Queue { get; init; } = null!;

    /// <summary>
    /// <see cref="IConsumer{TEvent}"/>.
    /// </summary>
    public Type Consumer { get; init; } = null!;

    /// <summary>
    /// Event model.
    /// </summary>
    public Type Event { get; init; } = null!;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
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
            return consumerType.Queue == this.Queue;
        }

        return false;
    }

    /// <inheritdoc/>
    public int CompareTo(ConsumerType? other)
    {
        return this.Queue.CompareTo(other?.Queue);
    }
}
