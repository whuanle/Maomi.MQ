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
/// The default consumer of MediatR's message queue publishes events after receiving messages.<br />
/// MediatR 的消息队列默认消费者，接收消息后发布事件.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class FastEndpointsConsumer<TCommand> : IConsumer<TCommand>
        where TCommand : class
{
    private readonly IEventMiddleware<TCommand> _eventMiddleware;
    private readonly ILogger<FastEndpointsConsumer<TCommand>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsConsumer{TCommand}"/> class.
    /// </summary>
    /// <param name="eventMiddleware"></param>
    /// <param name="logger"></param>
    public FastEndpointsConsumer(IEventMiddleware<TCommand> eventMiddleware, ILogger<FastEndpointsConsumer<TCommand>> logger)
    {
        _eventMiddleware = eventMiddleware;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(MessageHeader messageHeader, TCommand message)
    {
        EventHandlerDelegate<TCommand> eventHandlerDelegate = EventHandler;
        await _eventMiddleware.ExecuteAsync(messageHeader, message, eventHandlerDelegate);
    }

    /// <inheritdoc/>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TCommand message)
    {
        return _eventMiddleware.FallbackAsync(messageHeader, message, ex);
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TCommand? message, Exception? ex)
    {
        return _eventMiddleware.FallbackAsync(messageHeader, message, ex);
    }

    private async Task EventHandler(MessageHeader messageHeader, TCommand message, CancellationToken cancellationToken)
    {
        if (message is IEvent @event)
        {
            await @event.PublishAsync(waitMode: Mode.WaitForAll, cancellationToken);
        }
        else if (message is ICommand command)
        {
            await command.ExecuteAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning("The message type {MessageType} is not supported.Message :{@Message}", typeof(TCommand).Name, message);
            await Task.CompletedTask;
        }
    }
}
