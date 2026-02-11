// <copyright file="FastEndpointsMqCommandHandler{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// FastEndpoints MQ command handler.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class FastEndpointsMqCommandHandler<TCommand> : ICommandHandler<FastEndpointsMqCommand<TCommand>>
    where TCommand : class, ICommand
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsMqCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    public FastEndpointsMqCommandHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(FastEndpointsMqCommand<TCommand> command, CancellationToken ct)
    {
        return _messagePublisher.AutoPublishAsync(command.Command, cancellationToken: ct);
    }
}
