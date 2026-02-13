// <copyright file="DefaultFastEndpointsEventMiddleware{TCommand}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;

namespace Maomi.MQ;

/// <summary>
/// The default FastEndpoints message consumer middleware.
/// </summary>
/// <typeparam name="TCommand">Command.</typeparam>
public class DefaultFastEndpointsEventMiddleware<TCommand> : IEventMiddleware<TCommand>
    where TCommand : class
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <inheritdoc/>
    public virtual Task ExecuteAsync(MessageHeader messageHeader, TCommand message, EventHandlerDelegate<TCommand> next)
    {
        return next(messageHeader, message, _cancellationTokenSource.Token);
    }

    /// <inheritdoc/>
    public virtual Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TCommand? message)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TCommand? message, Exception? ex)
    {
        return Task.FromResult(ConsumerState.Ack);
    }
}
