// <copyright file="EventBusTransactionTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Attributes;
using Maomi.MQ.Consumer;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.Transaction.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maomi.MQ.Transaction;

/// <summary>
/// Registers eventbus consumers using transaction middleware.
/// </summary>
public class EventBusTransactionTypeFilter : ITypeFilter
{
    private readonly EventBusTypeFilter _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusTransactionTypeFilter"/> class.
    /// </summary>
    /// <param name="consumerInterceptor">Consumer interceptor.</param>
    public EventBusTransactionTypeFilter(ConsumerInterceptor? consumerInterceptor = null)
    {
        _inner = new EventBusTypeFilter(consumerInterceptor);
    }

    /// <inheritdoc/>
    public void Filter(IServiceCollection services, Type type)
    {
        _inner.Filter(services, type);
    }

    /// <inheritdoc/>
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        var consumerTypes = _inner.Build(services).ToList();

        foreach (var consumerType in consumerTypes)
        {
            var middlewareInterface = typeof(IEventMiddleware<>).MakeGenericType(consumerType.Event);
            var middlewareType = typeof(TransactionEventMiddleware<>).MakeGenericType(consumerType.Event);

            services.Replace(ServiceDescriptor.Scoped(middlewareInterface, middlewareType));
        }

        return consumerTypes;
    }
}
