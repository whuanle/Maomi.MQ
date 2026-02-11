// <copyright file="MediatRMqCommand{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;

namespace Maomi.MQ.MediatR;

/// <summary>
/// Send messages using RabbitMQ.<br />
/// 使用 RabbitMQ 发送消息.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatRMqCommand<TCommand> : IRequest
    where TCommand : class, IRequest
{
    /// <summary>
    /// Command message.
    /// </summary>
    public TCommand Message { get; init; } = default!;
}
