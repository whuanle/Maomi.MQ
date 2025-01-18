// <copyright file="IConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Custom consumer.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface IConsumer<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Message handler.<br />
    /// 消息处理.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message">事件内容.</param>
    /// <returns><see cref="Task"/>.</returns>
    public Task ExecuteAsync(MessageHeader messageHeader, TMessage message);

    /// <summary>
    /// Executed on each failure.If the exception is not caused by the ExecuteAsync method, such as a serialization error, then the retryCount = -1.<br />
    /// 每次消费失败时执行.如果异常不是因为 ExecuteAsync 方法导致的，例如序列化错误等，则 retryCount = -1.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="ex">An anomaly occurs when consuming.<br />消费时出现的异常.</param>
    /// <param name="retryCount">Current retry times.<br />当前重试次数.</param>
    /// <param name="message"></param>
    /// <returns><see cref="Task"/>.</returns>
    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message);

    /// <summary>
    /// Executed when the last retry fails.<br />
    /// 最后一次重试失败时执行.
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    /// <returns>Check whether the rollback is successful.</returns>
    public Task<FallbackState> FallbackAsync(MessageHeader messageHeader, TMessage message);
}
