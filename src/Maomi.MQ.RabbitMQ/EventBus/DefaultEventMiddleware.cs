// <copyright file="DefaultEventMiddleware.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <inheritdoc />
public class DefaultEventMiddleware<TMessage> : IEventMiddleware<TMessage>
        where TMessage : class
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <inheritdoc />
    public virtual Task ExecuteAsync(MessageHeader messageHeader, TMessage message, EventHandlerDelegate<TMessage> next)
    {
        return next.Invoke(messageHeader, message, _cancellationTokenSource.Token);
    }

    /// <inheritdoc />
    public virtual Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage? message)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
    {
        return Task.FromResult(ConsumerState.Ack);
    }
}
