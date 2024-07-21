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
    /// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
    /// <typeparam name="TEvent">Event type.</typeparam>
    /// <param name="consumerOptions"></param>
    /// <param name="stoppingToken"></param>
    /// <returns>If a consumer already exists in the queue, return false.<br />如果队列存在消费者，则返回添加失败.</returns>
    Task<bool> StartAsync<TConsumer, TEvent>(IConsumerOptions consumerOptions, CancellationToken stoppingToken = default)
        where TConsumer : class, IConsumer<TEvent>
        where TEvent : class;

    /// <summary>
    /// Stop consumer.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="stoppingToken"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task StopAsync(string queue, CancellationToken stoppingToken = default);
}