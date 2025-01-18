// <copyright file="IEventHandlerFactory{TMessage}.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event factory.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface IEventHandlerFactory<TMessage> : IEventHandlerFactory
    where TMessage : class
{
}
