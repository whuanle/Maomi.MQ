// <copyright file="EventBusConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus;

/// <summary>
/// Eventbus consumer.<br />
/// 事件总线消费者.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
public class EventBusConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
    private readonly IEventMiddleware<TEvent> _eventMiddleware;
    private readonly HandlerBroker<TEvent> _handlerBroker;
    private readonly ILogger<EventBusConsumer<TEvent>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusConsumer{TEvent}"/> class.
    /// </summary>
    /// <param name="eventMiddleware"></param>
    /// <param name="handlerBroker"></param>
    /// <param name="logger"></param>
    public EventBusConsumer(IEventMiddleware<TEvent> eventMiddleware, HandlerBroker<TEvent> handlerBroker, ILogger<EventBusConsumer<TEvent>> logger)
    {
        _eventMiddleware = eventMiddleware;
        _handlerBroker = handlerBroker;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(EventBody<TEvent> message)
    {
        await _eventMiddleware.HandleAsync(message, _handlerBroker.Handler);
    }

    /// <inheritdoc />
    public Task FaildAsync(EventBody<TEvent>? message)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task FallbackAsync(EventBody<TEvent>? message)
    {
        return Task.CompletedTask;
    }
}
