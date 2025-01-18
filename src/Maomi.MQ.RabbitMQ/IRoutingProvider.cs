// <copyright file="IRoutingProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Check exchange, routingKey, and queue.
/// </summary>
public interface IRoutingProvider
{
    /// <summary>
    /// Get options.
    /// </summary>
    /// <param name="consumerOptions"></param>
    /// <returns><see cref="IConsumerOptions"/>.</returns>
    IConsumerOptions Get(IConsumerOptions consumerOptions);
}
