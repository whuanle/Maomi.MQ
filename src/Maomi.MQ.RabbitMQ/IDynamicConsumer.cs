// <copyright file="IDynamicConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Dynamic consumer host.<br />
/// 动态消费者服务.
/// </summary>
public interface IDynamicConsumer
{
    /// <summary>
    /// Start new consumer.
    /// </summary>
    /// <typeparam name="TConsumer"><see cref="IConsumer{TMessage}"/>.</typeparam>
    /// <typeparam name="TMessage">Event type.</typeparam>
    /// <param name="consumerOptions"></param>
    /// <param name="stoppingToken"></param>
    /// <returns>If a consumer already exists in the queue, return false.<br />如果队列存在消费者，则返回添加失败.</returns>
    Task ConsumerAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
    where TMessage : class
    where TConsumer : class, IConsumer<TMessage>;

    Task ConsumerAsync<TMessage>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
        where TMessage : class;

    Task ConsumerAsync<TMessage>(
            IConsumerOptions consumerOptions,
            ConsumerExecuteAsync<TMessage> execute,
            ConsumerFaildAsync<TMessage>? faild = null,
            ConsumerFallbackAsync<TMessage>? fallback = null)
            where TMessage : class;

    /// <summary>
    /// Stop consumer.
    /// </summary>
    /// <param name="queue"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task StopConsumerAsync(string queue);
}