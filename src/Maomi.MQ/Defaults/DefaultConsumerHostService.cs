// <copyright file="DefaultConsumerHostService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Retry;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ.Defaults;

/// <summary>
/// Default consumer service.
/// </summary>
/// <typeparam name="TConsumer"><see cref="TConsumer"/>.</typeparam>
/// <typeparam name="TEvent"><see cref="TEvent"/>.</typeparam>
public class DefaultConsumerHostService<TConsumer, TEvent> : ConsumerBaseHostSrvice<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConsumerHostService{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="waitReadyFactory"></param>
    public DefaultConsumerHostService(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
        IRetryPolicyFactory policyFactory,
        IWaitReadyFactory waitReadyFactory)
        : this(
            serviceProvider,
            connectionOptions,
            jsonSerializer,
            logger,
            policyFactory,
            waitReadyFactory,
            GetConsumerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultConsumerHostService{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="waitReadyFactory"></param>
    /// <param name="consumerOptions"></param>
    protected DefaultConsumerHostService(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
        IRetryPolicyFactory policyFactory,
        IWaitReadyFactory waitReadyFactory,
        ConsumerOptions consumerOptions)
        : base(
            serviceProvider,
            connectionOptions,
            jsonSerializer,
            logger,
            policyFactory,
            consumerOptions,
            waitReadyFactory)
    {
    }

    /// <summary>
    /// Get options.
    /// </summary>
    /// <returns><see cref="ConsumerOptions"/>.</returns>
    protected static ConsumerOptions GetConsumerOptions()
    {
        var consumerAttribute = typeof(TConsumer).GetCustomAttribute<ConsumerAttribute>();
        if (consumerAttribute == null)
        {
            ArgumentNullException.ThrowIfNull(consumerAttribute);
        }

        return new ConsumerOptions
        {
            Qos = consumerAttribute.Qos,
            Queue = consumerAttribute.Queue,
            Requeue = consumerAttribute.Requeue
        };
    }
}
