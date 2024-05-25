// <copyright file="EventBusConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventMiddleware<TEvent> _eventMiddleware;
    private readonly IHandlerMediator<TEvent> _handlerBroker;
    private readonly ILogger<EventBusConsumer<TEvent>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusConsumer{TEvent}"/> class.
    /// </summary>
    /// <param name="eventMiddleware"></param>
    /// <param name="handlerBroker"></param>
    /// <param name="logger"></param>
    public EventBusConsumer(IEventMiddleware<TEvent> eventMiddleware, IHandlerMediator<TEvent> handlerBroker, ILogger<EventBusConsumer<TEvent>> logger, IServiceProvider serviceProvider)
    {
        _eventMiddleware = eventMiddleware;
        _handlerBroker = handlerBroker;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public virtual async Task ExecuteAsync(EventBody<TEvent> message)
    {
        await _eventMiddleware.ExecuteAsync(message, _handlerBroker.ExecuteAsync);
    }

    /// <inheritdoc />
    public virtual Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
    {
        return _eventMiddleware.FaildAsync(ex, retryCount, message);
    }

    /// <inheritdoc />
    public virtual Task<bool> FallbackAsync(EventBody<TEvent>? message)
    {
        return _eventMiddleware.FallbackAsync(message);
    }
}
