// <copyright file="EventHandlerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event factory.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
internal class EventHandlerFactory<TEvent> : IEventHandlerFactory<TEvent>
        where TEvent : class
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IReadOnlyDictionary<int, Type> Handlers { get; init; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHandlerFactory{TEvent}"/> class.
    /// </summary>
    /// <param name="handlers"></param>
    public EventHandlerFactory(IReadOnlyDictionary<int, Type> handlers)
    {
        Handlers = handlers;
    }
}