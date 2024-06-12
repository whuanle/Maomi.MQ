// <copyright file="IMessagePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Publish messagge.<br />
/// 消息发布者.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// MQ connection pool.
    /// </summary>
    public ConnectionPool ConnectionPool { get; }

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TEvent">Event model.<br />事件模型类.</typeparam>
    /// <param name="queue">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TEvent>(string queue, TEvent message, Action<BasicProperties>? properties = null)
        where TEvent : class;

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TEvent">Event model.<br />事件模型类.</typeparam>
    /// <param name="queue">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TEvent>(string queue, TEvent message, BasicProperties properties);

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TEvent">Event model.<br />事件模型类.</typeparam>
    /// <param name="queue">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties);
}
