// <copyright file="IRoutingProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Check exchange, routingKey, and queue.This interface will be called when publishing messages or creating consumer programs. You can re-implement this interface to intercept and modify the routing information.<br />
/// 在发布消息或创建消费者程序时会调用此接口，你可以重新实现此接口，以便拦截、修改路由信息.
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
