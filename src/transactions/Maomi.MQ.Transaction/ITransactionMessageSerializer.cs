// <copyright file="ITransactionMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction;

/// <summary>
/// Selects serializer for transaction publisher.
/// </summary>
public interface ITransactionMessageSerializer
{
    /// <summary>
    /// Gets serializer for message instance.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="message">Message instance.</param>
    /// <returns>Serializer.</returns>
    IMessageSerializer GetSerializer<TMessage>(TMessage message);
}
