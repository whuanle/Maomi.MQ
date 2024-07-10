// <copyright file="IEmptyConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Empty consumer, only the definition is created, no consumption is performed.<br />
/// 空消费者，只创建定义，不执行消费.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
public interface IEmptyConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
}

/// <summary>
/// Empty consumer, only the definition is created, no consumption is performed.<br />
/// 空消费者，只创建定义，不执行消费.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
public abstract class EmptyConsumer<TEvent> : IEmptyConsumer<TEvent>
    where TEvent : class
{
    /// <inheritdoc />
    public Task ExecuteAsync(EventBody<TEvent> message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<bool> FallbackAsync(EventBody<TEvent>? message)
    {
        throw new NotImplementedException();
    }
}
