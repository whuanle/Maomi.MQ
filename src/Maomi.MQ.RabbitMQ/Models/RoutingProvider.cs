// <copyright file="RoutingProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Models;

/// <summary>
/// Check exchange, routingKey, and queue.
/// </summary>
public class RoutingProvider : IRoutingProvider
{
    /// <inheritdoc />
    public IConsumerOptions Get(IConsumerOptions consumerOptions)
    {
        return consumerOptions;
    }

    /// <inheritdoc/>
    public IQueueNameOptions Get(IQueueNameOptions queueNameOptions)
    {
        return queueNameOptions;
    }
}
