// <copyright file="IHandlerMediator.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event mediator, used to generate a sequential event execution flow and compensation flow.<br />
/// 事件中介者，用于生成有顺序的事件执行流程和补偿流程.
/// </summary>
/// <typeparam name="TEvent">Event mode.</typeparam>
public interface IHandlerMediator<TEvent>
    where TEvent : class
{
    /// <summary>
    /// Execute the event, and the method will generate a <see cref="EventHandlerDelegate{TEvent}" /> delegate, passed to <see cref="IEventMiddleware{TEvent}"/>.<br />
    /// 执行事件，该方法会被生成 <see cref="EventHandlerDelegate{TEvent}" /> 委托，传递到 <see cref="IEventMiddleware{TEvent}"/> 中.
    /// </summary>
    /// <param name="eventBody"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteAsync(EventBody<TEvent> eventBody, CancellationToken cancellationToken);
}
