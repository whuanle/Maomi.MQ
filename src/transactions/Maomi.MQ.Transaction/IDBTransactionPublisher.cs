// <copyright file="IDBTransactionPublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction;

/// <summary>
/// RabbitMQ local transaction.
/// </summary>
public interface IDBTransactionPublisher : IMessagePublisher
{
}

/// <summary>
/// Alias of <see cref="IDBTransactionPublisher"/>.
/// </summary>
public interface ITransactionMessagePublisher : IDBTransactionPublisher
{
}
