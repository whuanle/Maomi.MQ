// <copyright file="EventBusConsumerHostSrvice.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Defaults;
using Maomi.MQ.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.EventBus;

/// <summary>
/// EventBus consumer host service.
/// </summary>
/// <typeparam name="TConsumer"><see cref="IConsumer{TEvent}"/>.</typeparam>
/// <typeparam name="TEvent">Event model.</typeparam>
public class EventBusConsumerHostSrvice<TConsumer, TEvent> : ConsumerBaseHostSrvice<TConsumer, TEvent>
    where TEvent : class
    where TConsumer : IConsumer<TEvent>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusConsumerHostSrvice{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="waitReadyFactory"></param>
    public EventBusConsumerHostSrvice(
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
            serviceProvider.GetRequiredKeyedService<ConsumerOptions>(typeof(TEvent)),
            waitReadyFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusConsumerHostSrvice{TConsumer, TEvent}"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="logger"></param>
    /// <param name="policyFactory"></param>
    /// <param name="consumerOptions"></param>
    /// <param name="waitReadyFactory"></param>
    protected EventBusConsumerHostSrvice(
        IServiceProvider serviceProvider,
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ILogger<ConsumerBaseHostSrvice<TConsumer, TEvent>> logger,
        IRetryPolicyFactory policyFactory,
        ConsumerOptions consumerOptions,
        IWaitReadyFactory waitReadyFactory)
        : base(serviceProvider, connectionOptions, jsonSerializer, logger, policyFactory, consumerOptions, waitReadyFactory)
    {
    }
}
