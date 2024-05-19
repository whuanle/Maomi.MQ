// <copyright file="HandlerMediator.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event mediator, used to generate a sequential event execution flow and compensation flow.<br />
/// 事件中介者，用于生成有顺序的事件执行流程和补偿流程.
/// </summary>
/// <typeparam name="TEvent">Event mode.</typeparam>
public class HandlerMediator<TEvent>
    where TEvent : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventInfo _eventInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerMediator{TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public HandlerMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _eventInfo = _serviceProvider.GetRequiredKeyedService<EventInfo>(typeof(TEvent));
    }

    /// <summary>
    /// Execute the event, and the method will generate a <see cref="EventHandlerDelegate{TEvent}" /> delegate, passed to <see cref="IEventMiddleware{TEvent}"/>.<br />
    /// 执行事件，该方法会被生成 <see cref="EventHandlerDelegate{TEvent}" /> 委托，传递到 <see cref="IEventMiddleware{TEvent}"/> 中.
    /// </summary>
    /// <param name="eventBody"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    public async Task Handler(EventBody<TEvent> eventBody, CancellationToken cancellationToken)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<TEvent>>();
        List<IEventHandler<TEvent>> eventHandlers = new List<IEventHandler<TEvent>>(_eventInfo.Handlers.Count);

        // Build execution flow.
        // 构建执行链.
        // 1 => 2 => 3 =>...
        foreach (var handler in _eventInfo.Handlers)
        {
            try
            {
                // Forward execution。
                // 正向执行.
                var eventHandler = _serviceProvider.GetRequiredService(handler.Value) as IEventHandler<TEvent>;
                ArgumentNullException.ThrowIfNull(eventHandler);

                eventHandlers.Add(eventHandler);
                await eventHandler.HandlerAsync(eventBody, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occurred while executing the event,event type:[{Name}], event id:[{Id}]", typeof(TEvent).Name, eventBody.Id);

                // Rollback.
                // 回滚.
                for (int j = eventHandlers.Count - 1; j >= 0; j--)
                {
                    var eventHandler = eventHandlers[j];
                    await eventHandler.CancelAsync(eventBody, cancellationToken);
                }

                return;
            }
        }
    }
}
