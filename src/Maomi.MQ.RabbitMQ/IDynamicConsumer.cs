// <copyright file="IDynamicConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Dynamic consumer services that dynamically start or stop consumers at run time.<br />
/// 动态消费者服务，在运行时动态启动或停止消费者.
/// </summary>
public interface IDynamicConsumer
{
    /// <summary>
    /// Launch a new consumer.<br />
    /// 启动一个新的消费者.
    /// </summary>
    /// <typeparam name="TConsumer"><see cref="IConsumer{TMessage}"/>.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="consumerOptions"></param>
    /// <returns>Return to Consumer identity.<br />返回消费者标识.</returns>
    Task<string> ConsumerAsync<TConsumer, TMessage>(IConsumerOptions consumerOptions)
    where TMessage : class
    where TConsumer : class, IConsumer<TMessage>;

    /// <summary>
    /// Launch a new consumer.<br />
    /// 启动一个新的消费者.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="consumerOptions"></param>
    /// <returns>Return to Consumer identity.<br />返回消费者标识.</returns>
    Task<string> ConsumerAsync<TMessage>(IConsumerOptions consumerOptions)
        where TMessage : class;

    /// <summary>
    /// Launch a new consumer.<br />
    /// 启动一个新的消费者.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="consumerOptions"></param>
    /// <param name="execute"><see cref="IConsumer{TMessage}.ExecuteAsync(MessageHeader, TMessage)"/>.</param>
    /// <param name="faild"><see cref="IConsumer{TMessage}.FaildAsync(MessageHeader, Exception, int, TMessage)"/>.</param>
    /// <param name="fallback"><see cref="IConsumer{TMessage}.FallbackAsync(MessageHeader, TMessage, Exception?)" />.</param>
    /// <returns>Return to Consumer identity.<br />返回消费者标识.</returns>
    Task<string> ConsumerAsync<TMessage>(
            IConsumerOptions consumerOptions,
            ConsumerExecuteAsync<TMessage> execute,
            ConsumerFaildAsync<TMessage>? faild = null,
            ConsumerFallbackAsync<TMessage>? fallback = null)
            where TMessage : class;

    /// <summary>
    /// Stop the consumer using the queue name.<br />
    /// 使用队列名称停止消费者.
    /// </summary>
    /// <param name="queue"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task StopConsumerAsync(string queue);

    /// <summary>
    /// Use the consumer logo to stop consumers.<see cref="IChannel.BasicCancelAsync"/> will be called to cancel the consumption.<br />
    /// 使用消费者标识停止消费者，将会调用 <see cref="IChannel.BasicCancelAsync"/> 取消消费.
    /// </summary>
    /// <param name="consumerTag"></param>
    /// <returns><see cref="Task"/>.</returns>
    Task StopConsumerTagAsync(string consumerTag);
}