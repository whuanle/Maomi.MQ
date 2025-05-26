// <copyright file="FastEndpointMQEventBusHandler.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using FastEndpoints;

namespace Maomi.MQ;

/// <summary>
/// FastEndpointMQEventBusHandler.
/// </summary>
/// <typeparam name="TEvent">Event.</typeparam>
public class FastEndpointMQEventBusHandler<TEvent> : FastEndpoints.IEventHandler<FeMQEvent<TEvent>>
        where TEvent : class, IEvent
{
    private readonly IMessagePublisher _messagePublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastEndpointMQEventBusHandler{TEvent}"/> class.
    /// </summary>
    /// <param name="messagePublisher"></param>
    public FastEndpointMQEventBusHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    /// <inheritdoc/>
    public Task HandleAsync(FeMQEvent<TEvent> eventModel, CancellationToken ct)
    {
        return _messagePublisher.AutoPublishAsync<TEvent>(message: eventModel.Event);
    }
}