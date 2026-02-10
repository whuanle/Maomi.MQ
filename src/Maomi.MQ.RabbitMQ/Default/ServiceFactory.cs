// <copyright file="ServiceFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

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
    /// <param name="serviceProvider"></param>
    /// <param name="retryPolicyFactory"></param>
    /// <param name="ids"></param>
    public ServiceFactory(
        IServiceProvider serviceProvider,
        MqOptions options,
        IRetryPolicyFactory retryPolicyFactory,
        IIdProvider ids)
    {
        ServiceProvider = serviceProvider;
        Options = options;
        RetryPolicyFactory = retryPolicyFactory;
        Ids = ids;
    }

    /// <summary>
    /// <see cref="MqOptions"/>.
    /// </summary>
    public MqOptions Options { get; private init; }

    /// <summary>
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; }

    /// <summary>
    /// <see cref="IIdProvider"/>.
    /// </summary>
    public IIdProvider Ids { get; private init; }

    /// <summary>
    /// <see cref="IRetryPolicyFactory"/>.
    /// </summary>
    public IRetryPolicyFactory RetryPolicyFactory { get; private init; }
}
