// <copyright file="DbTransactionTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Filters;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;
using System.Reflection;
using System.Transactions;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Scanning inherits the type of IDbTransactionConsumer.
/// </summary>
public class DbTransactionTypeFilter : ITypeFilter
{
    private readonly ConsumerInterceptor? _consumerInterceptor;
    private readonly HashSet<ConsumerType> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTransactionTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Filter.</param>
    /// <param name="eventMiddleware"></param>
    public DbTransactionTypeFilter(ConsumerInterceptor? consumerInterceptor = null, Type? eventMiddleware = null)
    {
        _consumerInterceptor = consumerInterceptor;
    }

    /// <inheritdoc/>
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        return _consumers.ToList();
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IDbTransactionConsumer<>).IsAssignableFrom(type))
        {
            return;
        }

        var tranConsumerInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbTransactionConsumer<>));
        if (tranConsumerInterface == null)
        {
            return;
        }

        IConsumerOptions? consumerAttribute = type.GetCustomAttribute<ConsumerAttribute>();

        if (consumerAttribute == null || string.IsNullOrEmpty(consumerAttribute.Queue))
        {
            return;
        }

        if (_consumerInterceptor != null)
        {
            var register = _consumerInterceptor.Invoke(consumerAttribute, type);
            if (!register.IsRegister)
            {
                return;
            }

            consumerAttribute = register.Options.Clone();
        }

        if (_consumers.FirstOrDefault(x => x.Queue == consumerAttribute.Queue) is ConsumerType existConsumerType)
        {
            throw new ArgumentException($"Repeat bound queue [{consumerAttribute.Queue}],{existConsumerType.Event.Name} and {type.Name}");
        }

        var consumerInterface = typeof(IConsumer<>).MakeGenericType(type);
        var implementationType = typeof(DbTransactionConsumer<>).MakeGenericType(type);

        services.AddScoped(tranConsumerInterface, type);
        services.TryAddEnumerable(new ServiceDescriptor(serviceType: consumerInterface, implementationType: implementationType, lifetime: ServiceLifetime.Scoped));
        services.Add(new ServiceDescriptor(serviceType: implementationType, implementationType: implementationType, lifetime: ServiceLifetime.Scoped));

        var eventType = type;
        var consumerType = new ConsumerType
        {
            Queue = consumerAttribute.Queue,
            Consumer = implementationType,
            Event = eventType,
            ConsumerOptions = consumerAttribute
        };

        _consumers.Add(consumerType);
    }
}
