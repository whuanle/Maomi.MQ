// <copyright file="ServiceFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ.Default;

/// <summary>
/// Centralize some of the services required by the program.
/// 集中提供程序所必须的一些服务.
/// </summary>
public class ServiceFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFactory"/> class.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serializer"></param>
    /// <param name="retryPolicyFactory"></param>
    /// <param name="waitReadyFactory"></param>
    /// <param name="circuitBreakerFactory"></param>
    /// <param name="ids"></param>
    public ServiceFactory(
        MqOptions options,
        IJsonSerializer serializer,
        IRetryPolicyFactory retryPolicyFactory,
        IWaitReadyFactory waitReadyFactory,
        ICircuitBreakerFactory circuitBreakerFactory,
        IIdFactory ids)
    {
        Options = options;
        Serializer = serializer;
        RetryPolicyFactory = retryPolicyFactory;
        WaitReadyFactory = waitReadyFactory;
        CircuitBreakerFactory = circuitBreakerFactory;
        Ids = ids;
    }

    /// <summary>
    /// <see cref="MqOptions"/>.
    /// </summary>
    public MqOptions Options { get; private init; }

    /// <summary>
    /// <see cref="IIdFactory"/>.
    /// </summary>
    public IIdFactory Ids { get; private init; }

    /// <summary>
    /// <see cref="IJsonSerializer"/>.
    /// </summary>
    public IJsonSerializer Serializer { get; private init; }

    /// <summary>
    /// <see cref="IRetryPolicyFactory"/>.
    /// </summary>
    public IRetryPolicyFactory RetryPolicyFactory { get; private init; }

    /// <summary>
    /// <see cref="IWaitReadyFactory"/>.
    /// </summary>
    public IWaitReadyFactory WaitReadyFactory { get; private init; }

    /// <summary>
    /// <see cref="ICircuitBreakerFactory"/>.
    /// </summary>
    public ICircuitBreakerFactory CircuitBreakerFactory { get; private init; }
}
