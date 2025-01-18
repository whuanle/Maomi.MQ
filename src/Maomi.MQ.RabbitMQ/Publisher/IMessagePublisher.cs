// <copyright file="IMessagePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Publish messagge.<br />
/// 消息发布者.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="exchange">Exchange name.<br /> 交换器名称.</param>
    /// <param name="routingKey">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, Action<BasicProperties>? properties = null)
        where TMessage : class;

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null)
        where TMessage : class;

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="exchange">Exchange name.<br /> 交换器名称.</param>
    /// <param name="routingKey">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default);

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(TMessage message, BasicProperties? properties = default);

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="exchange">Exchange name.<br /> 交换器名称.</param>
    /// <param name="routingKey">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <returns><see cref="Task"/>.</returns>
    Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default);
}
