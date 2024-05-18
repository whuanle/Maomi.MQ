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
    public Task HandleAsync(EventBody<TEvent> eventBody, EventHandlerDelegate<TEvent> next)
    {
        return next(eventBody, CancellationToken.None);
    }
}
