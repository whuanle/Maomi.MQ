// <copyright file="MediatrConsumer{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;
using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// The default consumer of MediatR's message queue publishes events after receiving messages.<br />
/// MediatR 的消息队列默认消费者，接收消息后发布事件.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatrConsumer<TCommand> : IConsumer<TCommand>
        where TCommand : class, IRequest
{
    private readonly IEventMiddleware<TCommand> _eventMiddleware;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatrConsumer{TCommand}"/> class.
    /// </summary>
    /// <param name="eventMiddleware"></param>
    /// <param name="mediator"></param>
    public MediatrConsumer(IEventMiddleware<TCommand> eventMiddleware, IMediator mediator)
    {
        _eventMiddleware = eventMiddleware;
        _mediator = mediator;
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
        await _mediator.Send(message, cancellationToken);
    }
}
