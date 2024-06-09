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
/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
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
    Task ExecuteAsync(EventBody<TEvent> eventBody, EventHandlerDelegate<TEvent> next);

    /// <summary>
    /// Executed on each failure.If the exception is not caused by the ExecuteAsync method, such as a serialization error, then the retryCount = -1.<br />
    /// 每次消费失败时执行.如果异常不是因为 ExecuteAsync 方法导致的，例如序列化错误等，则 retryCount = -1.
    /// </summary>
    /// <param name="ex">An anomaly occurs when consuming.<br />消费时出现的异常.</param>
    /// <param name="retryCount">Current retry times.<br />当前重试次数.</param>
    /// <param name="message"></param>
    /// <returns><see cref="Task"/>.</returns>
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message);

    /// <summary>
    /// Executed when the last retry fails.<br />
    /// 最后一次重试失败时执行.
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Check whether the rollback is successful.</returns>
    public Task<bool> FallbackAsync(EventBody<TEvent>? message);
}
