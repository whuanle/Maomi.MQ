// <copyright file="FastEndpointsConsumer{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ;

/// <summary>
/// FastEndpoints queue consumer.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public class FastEndpointsConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    private readonly IEventMiddleware<TMessage> _eventMiddleware;
    private readonly ILogger<FastEndpointsConsumer<TMessage>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsConsumer{TMessage}"/> class.
    /// </summary>
    /// <param name="eventMiddleware">Event middleware.</param>
    /// <param name="logger">Logger.</param>
    public FastEndpointsConsumer(IEventMiddleware<TMessage> eventMiddleware, ILogger<FastEndpointsConsumer<TMessage>> logger)
    {
        _eventMiddleware = eventMiddleware;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
    {
        return _eventMiddleware.ExecuteAsync(messageHeader, message, EventHandlerAsync);
    }

    /// <inheritdoc/>
    public async Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    {
        await _eventMiddleware.FaildAsync(messageHeader, ex, retryCount, message);
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        return _eventMiddleware.FallbackAsync(messageHeader, message, ex);
    }

    private Task EventHandlerAsync(MessageHeader messageHeader, TMessage message, CancellationToken cancellationToken)
    {
        return message switch
        {
            IEvent @event => @event.PublishAsync(waitMode: Mode.WaitForAll, cancellationToken),
            ICommand command => command.ExecuteAsync(cancellationToken),
            _ => HandleUnsupportedMessageAsync(message),
        };
    }

    private Task HandleUnsupportedMessageAsync(TMessage message)
    {
        _logger.LogWarning("The message type {MessageType} is not supported. Message: {@Message}", typeof(TMessage).Name, message);
        return Task.CompletedTask;
    }
}
