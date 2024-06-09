// <copyright file="MaomiCoreExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ;

/// <summary>
/// Extensnions.
/// </summary>
public static class MaomiCoreExtensions
{
    /// <summary>
    /// Add base services.<br />
    /// 添加基础服务.
    /// </summary>
    /// <param name="services"></param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQCore(this IServiceCollection services)
    {
        services.AddSingleton<IJsonSerializer, DefaultJsonSerializer>();
        services.AddSingleton<IWaitReadyFactory, DefaultWaitReadyFactory>();
        services.AddSingleton<IRetryPolicyFactory, DefaultRetryPolicyFactory>();
        services.AddSingleton<ICircuitBreakerFactory, DefaultCircuitBreakerFactory>();
        return services;
    }
}
