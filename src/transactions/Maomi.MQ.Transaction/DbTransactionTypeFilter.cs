// <copyright file="DbTransactionTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.Filters;
using Maomi.MQ.Transaction.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Scans and registers <see cref="IDbTransactionConsumer{TMessage}"/> consumers.
/// </summary>
public class DbTransactionTypeFilter : ITypeFilter
{
    private readonly ConsumerInterceptor? _consumerInterceptor;
    private readonly HashSet<ConsumerType> _consumers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTransactionTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Consumer interceptor.</param>
    public DbTransactionTypeFilter(ConsumerInterceptor? consumerInterceptor = null)
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
        if (!type.IsClass || type.IsAbstract)
        {
            return;
        }

        var transactionConsumerInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbTransactionConsumer<>));

        if (transactionConsumerInterface == null)
        {
            return;
        }

        IConsumerOptions? consumerOptions = type.GetCustomAttribute<ConsumerAttribute>();
        if (consumerOptions == null || string.IsNullOrWhiteSpace(consumerOptions.Queue))
        {
            return;
        }

        if (_consumerInterceptor != null)
        {
            var register = _consumerInterceptor.Invoke(consumerOptions, type);
            if (!register.IsRegister)
            {
                return;
            }

            consumerOptions = register.ConsumerOptions.Clone();
        }

        if (_consumers.Any(x => x.Queue == consumerOptions.Queue))
        {
            throw new ArgumentException($"Multiple consumers are bound to the same queue. queue: [{consumerOptions.Queue}], consumer: {type.Name}");
        }

        var messageType = transactionConsumerInterface.GenericTypeArguments[0];
        var wrapperType = typeof(DbTransactionConsumer<>).MakeGenericType(messageType);
        var consumerInterface = typeof(IConsumer<>).MakeGenericType(messageType);

        services.AddScoped(transactionConsumerInterface, type);
        services.AddScoped(wrapperType, serviceProvider =>
        {
            var inner = serviceProvider.GetRequiredService(transactionConsumerInterface);

            return ActivatorUtilities.CreateInstance(
                serviceProvider,
                wrapperType,
                inner,
                consumerOptions.Queue);
        });

        services.Replace(ServiceDescriptor.Scoped(consumerInterface, serviceProvider => serviceProvider.GetRequiredService(wrapperType)));

        _consumers.Add(new ConsumerType
        {
            Queue = consumerOptions.Queue,
            Consumer = wrapperType,
            Event = messageType,
            ConsumerOptions = consumerOptions
        });
    }
}
