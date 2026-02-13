// <copyright file="IDbTransactionConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction;

/// <summary>
/// IDbTransactionConsumer interface.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface IDbTransactionConsumer<TMessage> : IBasicMessageHandler<TMessage>
    where TMessage : class
{
}

/// <summary>
/// Alias of <see cref="IDbTransactionConsumer{TMessage}"/>.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface ITransactionConsumer<TMessage> : IDbTransactionConsumer<TMessage>
    where TMessage : class
{
}
