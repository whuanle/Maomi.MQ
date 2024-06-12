// <copyright file="IEventHandlerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Event factory.
/// </summary>
public interface IEventHandlerFactory
{
    /// <summary>
    /// Handlers.
    /// </summary>
    public IReadOnlyDictionary<int, Type> Handlers { get; }
}
