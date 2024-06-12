// <copyright file="RedisRetryExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Maomi.MQ;

/// <summary>
/// Redis retry extensions.<br />
/// Redis 重试扩展.
/// </summary>
public static class RedisRetryExtensions
{
    /// <summary>
    /// Redis retry extensions.<br />
    /// Redis 重试扩展.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="func">Create redis client.<br />创建 Redis 连接的委托.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMaomiMQRedisRetry(this IServiceCollection services, Func<IServiceProvider, IDatabase> func)
    {
        services.AddSingleton<IRetryPolicyFactory, RedisRetryPolicyFactory>(s =>
        {
            var logger = s.GetRequiredService<ILogger<DefaultRetryPolicyFactory>>();
            var redis = func.Invoke(s);
            var mqOptions = s.GetRequiredService<MqOptions>();
            return new RedisRetryPolicyFactory(logger, redis);
        });
        return services;
    }
}
