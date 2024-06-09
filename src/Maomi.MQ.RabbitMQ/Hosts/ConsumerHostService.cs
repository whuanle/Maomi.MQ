// <copyright file="ConsumerHostService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1649
#pragma warning disable SA1401
#pragma warning disable SA1600

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Base consumer service.
/// </summary>
/// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
/// <typeparam name="TEvent">Event model.</typeparam>
public class ConsumerHostService<TConsumer, TEvent> : ConsumerBaseHostService
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerHostService{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="serviceFactory"></param>
    /// <param name="logger"></param>
    public ConsumerHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ILogger<ConsumerBaseHostService> logger)
        : base(serviceProvider, serviceFactory, logger, GetConsumerType())
    {
    }

    /// <summary>
    /// Get options.
    /// </summary>
    /// <returns><see cref="ConsumerOptions"/>.</returns>
    public static IReadOnlyList<ConsumerType> GetConsumerType()
    {
        var consumerAttribute = typeof(TConsumer).GetCustomAttribute<ConsumerAttribute>();
        if (consumerAttribute == null)
        {
            ArgumentNullException.ThrowIfNull(consumerAttribute);
        }

        return new List<ConsumerType>
        {
            new ConsumerType
            {
                Queue = consumerAttribute.Queue,
                Consumer = typeof(TConsumer),
                Event = typeof(TEvent)
            }
        };
    }
}
