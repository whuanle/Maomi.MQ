// <copyright file="FastEndpointsMqEventHandler{TEvent}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// FastEndpoints MQ event handler.
/// </summary>
/// <typeparam name="TEvent">Event.</typeparam>
public class FastEndpointsMqEventHandler<TEvent> : IEventHandler<FastEndpointsMqEvent<TEvent>>
    where TEvent : class, IEvent
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointsMqEventHandler{TEvent}"/> class.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    public FastEndpointsMqEventHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task HandleAsync(FastEndpointsMqEvent<TEvent> eventModel, CancellationToken ct)
    {
        return _messagePublisher.AutoPublishAsync(eventModel.Event, cancellationToken: ct);
    }
}
