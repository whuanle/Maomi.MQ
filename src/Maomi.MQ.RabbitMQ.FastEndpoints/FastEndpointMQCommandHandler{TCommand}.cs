// <copyright file="FastEndpointMQCommandHandler{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// FastEndpointMQCommandHandler.
/// </summary>
/// <typeparam name="TCommand">Event.</typeparam>
public class FastEndpointMQCommandHandler<TCommand> : FastEndpoints.ICommandHandler<FeMQCommand<TCommand>>
        where TCommand : class, ICommand
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointMQCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher"></param>
    public FastEndpointMQCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(FeMQCommand<TCommand> command, CancellationToken ct)
    {
        return _messagePublisher.AutoPublishAsync<TCommand>(message: command.Command);
    }
}
