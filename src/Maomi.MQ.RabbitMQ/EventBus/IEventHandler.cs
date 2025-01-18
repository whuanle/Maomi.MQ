// <copyright file="IEventHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// 事件执行器接口.
/// </summary>
/// <typeparam name="TMessage">事件模型.</typeparam>
public interface IEventHandler<TMessage>
{
    /// <summary>
    /// Forward execution event.<br />
    /// 正向执行事件.
    /// </summary>
    /// <param name="event">Event object.<br />事件对象.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteAsync(TMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// 补偿事件.
    /// </summary>
    /// <param name="event">Event object.<br />事件对象.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task CancelAsync(TMessage message, CancellationToken cancellationToken);
}
