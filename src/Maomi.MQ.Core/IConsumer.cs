// <copyright file="IConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Consumer abstract interface.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface IConsumer<TMessage> : IBasicMessageHandler<TMessage>
    where TMessage : class
{
}
