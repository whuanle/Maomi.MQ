// <copyright file="DefaultEventMiddleware.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <inheritdoc />
public class DefaultEventMiddleware<TEvent> : IEventMiddleware<TEvent>
{
    /// <inheritdoc />
    public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> FallbackAsync(EventBody<TEvent>? message)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(EventBody<TEvent> eventBody, EventHandlerDelegate<TEvent> next)
    {
        return next(eventBody, CancellationToken.None);
    }
}
