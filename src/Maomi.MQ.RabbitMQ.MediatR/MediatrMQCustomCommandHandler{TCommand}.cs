// <copyright file="MediatrMQCustomCommandHandler{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// RabbitCommandHandler.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatrMQCustomCommandHandler<TCommand> : IRequestHandler<MediatrMQCustomCommand<TCommand>>
        where TCommand : class, IRequest
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatrMQCustomCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher"></param>
    public MediatrMQCustomCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task Handle(MediatrMQCustomCommand<TCommand> request, CancellationToken cancellationToken)
    {
        if (request.Options == null)
        {
            return _messagePublisher.PublishAsync(
                request.Exchange,
                request.RoutingKey,
                request.Message,
                request.Properties,
                cancellationToken);
        }
        else
        {
            return _messagePublisher.PublishAsync(
                request.Exchange,
                request.RoutingKey,
                request.Message,
                request.Options,
                cancellationToken);
        }
    }
}
