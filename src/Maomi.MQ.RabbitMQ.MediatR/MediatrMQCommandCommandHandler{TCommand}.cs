// <copyright file="MediatrMQCommandCommandHandler{TCommand}.cs" company="Maomi">
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
public class MediatrMQCommandCommandHandler<TCommand> : IRequestHandler<MediatrMQCommand<TCommand>>
        where TCommand : class, IRequest
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatrMQCommandCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher"></param>
    public MediatrMQCommandCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task Handle(MediatrMQCommand<TCommand> request, CancellationToken cancellationToken)
    {
        return _messagePublisher.PublishAsync<TCommand>(model: request.Message);
    }
}