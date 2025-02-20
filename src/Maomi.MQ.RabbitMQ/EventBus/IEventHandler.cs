// <copyright file="IEventHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Represents the completion of one of the steps of an event.<br />
/// 表示完成一个事件的其中一个步骤.
/// </summary>
/// <typeparam name="TMessage">Message body.</typeparam>
public interface IEventHandler<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Forward execution event.<br />
    /// 正向执行事件.
    /// </summary>
    /// <param name="message">Message object.<br />事件对象.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteAsync(TMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// How do I roll back the current step when event execution fails.<br />
    /// 当事件执行失败时，如何回滚当前步骤.
    /// </summary>
    /// <param name="message">Message object.<br />事件对象.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task CancelAsync(TMessage message, CancellationToken cancellationToken);
}
