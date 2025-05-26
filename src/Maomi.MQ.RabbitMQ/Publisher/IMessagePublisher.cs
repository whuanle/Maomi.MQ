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
    /// When publishing a message, it will automatically query the corresponding routing information based on the consumers bound to this event, or the event bus, or other components supported (such as MediatR), thus avoiding the need to manually fill in the exchange and queue information..<br />
    /// 发布消息时，会自动根据该事件绑定的消费者、或者事件总线、或其它组件支持(MediatR)等，查询对应的路由信息，避免手动填写交换器和队列信息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task AutoPublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null, CancellationToken cancellationToken = default)
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
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, Action<BasicProperties> properties, CancellationToken cancellationToken = default)
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
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task PublishModelAsync<TMessage>(TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TMessage">Event model.<br />事件模型类.</typeparam>
    /// <param name="exchange">Exchange name.<br /> 交换器名称.</param>
    /// <param name="routingKey">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default);
}
