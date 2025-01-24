// <copyright file="DelegateHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;

namespace Maomi.MQ;

/// <inheritdoc cref="IConsumer{TMessage}.ExecuteAsync(Maomi.MQ.MessageHeader, TMessage)"/>.
public delegate Task ConsumerExecuteAsync<TMessage>(MessageHeader messageHeader, TMessage message)
    where TMessage : class;

/// <inheritdoc cref="IConsumer{TMessage}.FaildAsync(MessageHeader, Exception, int, TMessage)"/>.
public delegate Task ConsumerFaildAsync<TMessage>(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
    where TMessage : class;

/// <inheritdoc cref="IConsumer{TMessage}.FallbackAsync(MessageHeader, TMessage, Exception?)"/>.
public delegate Task<ConsumerState> ConsumerFallbackAsync<TMessage>(MessageHeader messageHeader, TMessage? message, Exception? ex)
    where TMessage : class;

/// <summary>
/// Event execution chain delegate.<br />
/// 事件执行链委托.
/// </summary>
/// <typeparam name="TMessage">事件模型.</typeparam>
/// <param name="messageHeader"></param>
/// <param name="message">事件对象.</param>
/// <param name="cancellationToken"></param>
/// <returns><see cref="Task"/>.</returns>
public delegate Task EventHandlerDelegate<TMessage>(MessageHeader messageHeader, TMessage message, CancellationToken cancellationToken);

/// <summary>
/// <see cref="EventTopicAttribute"/> filter.
/// </summary>
/// <remarks>You can modify related parameters.<br />可以修改相关参数.</remarks>
/// <param name="consumerOptions"></param>
/// <param name="eventType"></param>
/// <returns>Whether to register the event.<br />是否注册该事件.</returns>
public delegate RegisterQueue EventTopicInterceptor(IConsumerOptions consumerOptions, Type eventType);

/// <summary>
/// <see cref="ConsumerInterceptor"/> filter.
/// </summary>
/// <remarks>You can modify related parameters.<br />可以修改相关参数.</remarks>
/// <param name="consumerOptions"></param>
/// <param name="consumerType"></param>
/// <returns>Whether to register the consumer.<br />是否注册该消费者.</returns>
public delegate RegisterQueue ConsumerInterceptor(IConsumerOptions consumerOptions, Type consumerType);