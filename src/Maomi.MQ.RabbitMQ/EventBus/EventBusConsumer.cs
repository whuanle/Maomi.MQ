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
/// <typeparam name="TMessage">Event model.</typeparam>
public class EventBusConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly IEventMiddleware<TMessage> _eventMiddleware;
    private readonly IHandlerMediator<TMessage> _handlerBroker;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusConsumer{TMessage}"/> class.
    /// </summary>
    /// <param name="eventMiddleware"></param>
    /// <param name="handlerBroker"></param>
    /// <param name="loggerFactory"></param>
    public EventBusConsumer(IEventMiddleware<TMessage> eventMiddleware, IHandlerMediator<TMessage> handlerBroker, ILoggerFactory loggerFactory)
    {
        _eventMiddleware = eventMiddleware;
        _handlerBroker = handlerBroker;
        _logger = loggerFactory.CreateLogger(Diagnostics.DiagnosticName.EventBus);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
    {
        return _eventMiddleware.ExecuteAsync(messageHeader, message, _handlerBroker.ExecuteAsync);
    }

    /// <inheritdoc />
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    {
        return _eventMiddleware.FaildAsync(messageHeader, ex, retryCount, message);
    }

    /// <inheritdoc />
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        return _eventMiddleware.FallbackAsync(messageHeader, message, ex);
    }
}
