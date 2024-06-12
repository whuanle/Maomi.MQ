// <copyright file="IEventHandlerFactory{TEvent}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event factory.
/// </summary>
/// <typeparam name="TEvent">Event model.</typeparam>
public interface IEventHandlerFactory<TEvent> : IEventHandlerFactory
    where TEvent : class
{
}
