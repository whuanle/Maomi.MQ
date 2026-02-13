// <copyright file="MediatRMqCustomCommandHandler{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// MediatR MQ custom command handler.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatRMqCustomCommandHandler<TCommand> : IRequestHandler<MediatRMqCustomCommand<TCommand>>
    where TCommand : class, IRequest
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatRMqCustomCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    public MediatRMqCustomCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task Handle(MediatRMqCustomCommand<TCommand> request, CancellationToken cancellationToken)
    {
        if (request.Options != null)
        {
            return _messagePublisher.PublishAsync(
                request.Exchange,
                request.RoutingKey,
                request.Message,
                request.Options,
                cancellationToken);
        }

        return _messagePublisher.CustomPublishAsync(
            request.Exchange,
            request.RoutingKey,
            request.Message,
            request.Properties,
            cancellationToken);
    }
}
