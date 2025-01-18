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
/// <typeparam name="TMessage">Event mode.</typeparam>
public interface IHandlerMediator<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Execute the event, and the method will generate a <see cref="EventHandlerDelegate{TMessage}" /> delegate, passed to <see cref="IEventMiddleware{TMessage}"/>.<br />
    /// 执行事件，该方法会被生成 <see cref="EventHandlerDelegate{TMessage}" /> 委托，传递到 <see cref="IEventMiddleware{TMessage}"/> 中.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task ExecuteAsync(MessageHeader messageHeader, TMessage message, CancellationToken cancellationToken);
}
