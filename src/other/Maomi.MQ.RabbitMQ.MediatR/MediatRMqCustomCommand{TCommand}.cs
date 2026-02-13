// <copyright file="MediatRMqCustomCommand{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;
using RabbitMQ.Client;

namespace Maomi.MQ.MediatR;

/// <summary>
/// Send messages using RabbitMQ custom exchange and routing key.<br />
/// 使用 RabbitMQ 自定义 exchange 和 routing key 发送消息.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatRMqCustomCommand<TCommand> : IRequest
    where TCommand : class, IRequest
{
    /// <summary>
    /// Exchange name.
    /// </summary>
    public string Exchange { get; init; } = string.Empty;

    /// <summary>
    /// Routing key.
    /// </summary>
    public string RoutingKey { get; init; } = string.Empty;

    /// <summary>
    /// Command message.
    /// </summary>
    public TCommand Message { get; init; } = default!;

    /// <summary>
    /// RabbitMQ basic properties.
    /// </summary>
    public BasicProperties? Properties { get; init; }

    /// <summary>
    /// Basic properties options callback.
    /// </summary>
    public Action<BasicProperties>? Options { get; init; }
}
