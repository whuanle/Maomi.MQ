// <copyright file="EventHandlerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event factory.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public class EventHandlerFactory<TMessage> : IEventHandlerFactory<TMessage>
        where TMessage : class
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public IReadOnlyDictionary<int, Type> Handlers { get; init; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHandlerFactory{TMessage}"/> class.
    /// </summary>
    /// <param name="handlers"></param>
    public EventHandlerFactory(IReadOnlyDictionary<int, Type> handlers)
    {
        Handlers = handlers;
    }
}