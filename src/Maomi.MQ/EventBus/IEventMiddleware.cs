// <copyright file="IEventMiddleware.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event execution chain delegate.<br />
/// 事件执行链委托.
/// </summary>
/// <typeparam name="TEvent">事件模型.</typeparam>
/// <param name="event">事件对象.</param>
/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
/// <returns><see cref="Task"/>.</returns>
public delegate Task EventHandlerDelegate<TEvent>(EventBody<TEvent> @event, CancellationToken cancellationToken);

/// <summary>
/// 事件中间件.
/// </summary>
/// <typeparam name="TEvent">事件模型.</typeparam>
public interface IEventMiddleware<TEvent>
{
    /// <summary>
    /// 处理事件.
    /// </summary>
    /// <param name="eventBody">事件内容.</param>
    /// <param name="next">事件执行链委托.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task HandleAsync(EventBody<TEvent> eventBody, EventHandlerDelegate<TEvent> next);
}
