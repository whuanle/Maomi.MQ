// <copyright file="IBreakdown.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client.Events;

namespace Maomi.MQ;

/// <summary>
/// Fault processor, which calls the relevant interface when the program fails.<br />
/// 故障处理器，当程序出现故障时会调用相关接口.
/// </summary>
public interface IBreakdown
{
    /// <summary>
    /// When there is no suitable customer.<br />
    /// 没有找到合适消费者时.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="messageType"></param>
    /// <param name="consumerType"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task NotFoundConsumerAsync(string queue, Type messageType, Type consumerType);

    /// <summary>
    /// Handle unrouteable messages.<br />
    /// 处理不可路由消息.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="event"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task BasicReturnAsync(object sender, BasicReturnEventArgs @event);
}