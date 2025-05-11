// <copyright file="MediatrMQCustomCommand.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using MediatR;
using RabbitMQ.Client;

namespace Maomi.MQ.MediatR;

/// <summary>
/// Send messages using RabbitMQ.<br />
/// 使用 RabbitMQ 发送消息.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class MediatrMQCustomCommand<TCommand> : IRequest
    where TCommand : class, IRequest
{
    /// <summary>
    /// Exchange name.<br /> 交换器名称.
    /// </summary>
    public string Exchange { get; init; } = default!;

    /// <summary>
    /// Queue name.<br />队列名称.
    /// </summary>
    public string RoutingKey { get; init; } = default!;

    /// <summary>
    /// Event object.<br />事件对象.
    /// </summary>
    public TCommand Message { get; init; } = default!;

    /// <summary>
    /// <see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.
    /// </summary>
    public BasicProperties? Properties { get; init; }

    /// <summary>
    /// <see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.
    /// </summary>
    public Action<BasicProperties>? Options { get; init; }
}
