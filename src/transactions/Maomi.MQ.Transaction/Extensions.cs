// <copyright file="Extensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.Transaction;
using Maomi.MQ.Transaction.Backgroud;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Default;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace Maomi.MQ;

/// <summary>
/// Extensions for Maomi MQ transaction support.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers Maomi MQ transaction module.
    /// </summary>
    /// <param name="services">Services.</param>
    /// <param name="options">Configure options.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQTransaction(this IServiceCollection services, Action<MQTransactionOptions> options)
    {
        MQTransactionOptions transactionOptions = new();
        options.Invoke(transactionOptions);

        if (string.IsNullOrWhiteSpace(transactionOptions.ProviderName))
        {
            throw new InvalidOperationException($"{nameof(MQTransactionOptions.ProviderName)} is required.");
        }

        services.AddSingleton<IMQTransactionOptions>(transactionOptions);
        services.AddScoped<IDatabaseProvider>(DbProviderResolver.Resolve);
        services.AddScoped<ITransactionMessageSerializer, TransactionMessageSerializer>();
        services.AddHostedService<PublisherBackgroundService>();

        return services;
    }

    /// <summary>
    /// Creates a transaction publisher that writes outbox rows in ambient transaction.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    /// <returns><see cref="IDBTransactionPublisher"/>.</returns>
    public static IDBTransactionPublisher CreateDBTransaction(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            return new DBTransactionPublisher(publisher);
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// Creates a transaction publisher using explicit db transaction.
    /// </summary>
    /// <param name="messagePublisher">Message publisher.</param>
    /// <param name="dbConnection">Database connection.</param>
    /// <param name="dbTransaction">Database transaction.</param>
    /// <returns><see cref="IDBTransactionPublisher"/>.</returns>
    public static IDBTransactionPublisher CreateDBTransaction(this IMessagePublisher messagePublisher, DbConnection dbConnection, DbTransaction dbTransaction)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            return new DBTransactionPublisher(publisher, dbConnection, dbTransaction);
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// Builds transaction filters for <see cref="AddMaomiMQ(IServiceCollection, Action{MqOptionsBuilder}, System.Reflection.Assembly[], ITypeFilter[])"/>.
    /// </summary>
    /// <param name="consumerInterceptor">Consumer interceptor.</param>
    /// <returns>Type filters.</returns>
    public static ITypeFilter[] CreateTransactionFilters(ConsumerInterceptor? consumerInterceptor = null)
    {
        return new ITypeFilter[]
        {
            new DbTransactionTypeFilter(consumerInterceptor),
            new EventBusTransactionTypeFilter(consumerInterceptor)
        };
    }

    /// <summary>
    /// Creates eventbus middleware wrapped with transaction inbox barrier.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddEventBusTransactionMiddleware<TMessage>(this IServiceCollection services)
        where TMessage : class
    {
        services.AddScoped<IEventMiddleware<TMessage>, TransactionEventMiddleware<TMessage>>();
        return services;
    }
}
