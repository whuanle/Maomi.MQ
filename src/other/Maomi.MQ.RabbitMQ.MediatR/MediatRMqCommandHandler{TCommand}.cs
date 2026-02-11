// <copyright file="MediatRMqCommandHandler{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// MediatR MQ command handler.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatRMqCommandHandler<TCommand> : IRequestHandler<MediatRMqCommand<TCommand>>
    where TCommand : class, IRequest
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatRMqCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    public MediatRMqCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task Handle(MediatRMqCommand<TCommand> request, CancellationToken cancellationToken)
    {
        return _messagePublisher.AutoPublishAsync(request.Message, cancellationToken: cancellationToken);
    }
}
