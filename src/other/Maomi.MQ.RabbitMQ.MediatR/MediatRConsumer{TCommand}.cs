// <copyright file="MediatRConsumer{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;
using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// MediatR queue consumer.
/// </summary>
/// <typeparam name="TCommand">Command type.</typeparam>
public class MediatRConsumer<TCommand> : IConsumer<TCommand>
    where TCommand : class, IRequest
{
    private readonly IEventMiddleware<TCommand> _eventMiddleware;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatRConsumer{TCommand}"/> class.
    /// </summary>
    /// <param name="eventMiddleware">Event middleware.</param>
    /// <param name="mediator">Mediator.</param>
    public MediatRConsumer(IEventMiddleware<TCommand> eventMiddleware, IMediator mediator)
    {
        _eventMiddleware = eventMiddleware;
        _mediator = mediator;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(MessageHeader messageHeader, TCommand message)
    {
        return _eventMiddleware.ExecuteAsync(messageHeader, message, EventHandlerAsync);
    }

    /// <inheritdoc/>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TCommand message)
    {
        return _eventMiddleware.FaildAsync(messageHeader, ex, retryCount, message);
    }

    /// <inheritdoc/>
    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TCommand? message, Exception? ex)
    {
        return _eventMiddleware.FallbackAsync(messageHeader, message, ex);
    }

    private Task EventHandlerAsync(MessageHeader messageHeader, TCommand message, CancellationToken cancellationToken)
    {
        return _mediator.Send(message, cancellationToken);
    }
}
