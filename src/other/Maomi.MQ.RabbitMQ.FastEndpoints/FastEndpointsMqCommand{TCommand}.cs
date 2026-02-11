// <copyright file="FastEndpointsMqCommand{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// Send FastEndpoints command to the message queue.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class FastEndpointsMqCommand<TCommand> : ICommand
    where TCommand : ICommand
{
    /// <summary>
    /// Command.
    /// </summary>
    public TCommand Command { get; init; } = default!;
}
