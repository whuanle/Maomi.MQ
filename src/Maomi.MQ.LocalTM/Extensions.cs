// <copyright file="Extensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction;
using Maomi.MQ.Transaction.Backgroud;
using Maomi.MQ.Transaction.Default;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Data.Common;

namespace Maomi.MQ;

/// <summary>
/// Extensions for Maomi MQ transaction support.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Add Maomi MQ transaction support.<br />
    /// </summary>
    /// <param name="services"></param>
    /// <param name="options"></param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQTransaction(this IServiceCollection services, Action<MQTransactionOptions> options)
    {
        MQTransactionOptions transactionOptions = new();
        options.Invoke(transactionOptions);

        services.AddSingleton<IMQTransactionOptions>(transactionOptions);
        services.AddHostedService<PublisherBackgroundService>();

        return services;
    }

    /// <summary>
    /// See <see cref="IChannel.TxSelectAsync(CancellationToken)"/>. <br />
    /// Asynchronously enable TX mode for this session.<br />
    /// 开启事务.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static IDBTransactionPublisher CreateDBTransaction(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var tran = new DBTransactionPublisher(publisher);
            return tran;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// See <see cref="IChannel.TxSelectAsync(CancellationToken)"/>. <br />
    /// Asynchronously enable TX mode for this session.<br />
    /// 开启事务.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    /// <param name="dbConnection"></param>
    /// <param name="dbTransaction"></param>
    public static IDBTransactionPublisher CreateDBTransaction(this IMessagePublisher messagePublisher, DbConnection dbConnection, DbTransaction dbTransaction)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var tran = new DBTransactionPublisher(publisher, dbConnection, dbTransaction);
            return tran;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }
}
