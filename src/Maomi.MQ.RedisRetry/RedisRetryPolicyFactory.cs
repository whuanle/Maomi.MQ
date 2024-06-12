// <copyright file="RedisRetryPolicyFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Maomi.MQ;

/// <summary>
/// Retry policy factory.
/// </summary>
public class RedisRetryPolicyFactory : IRetryPolicyFactory
{
    private const int MaxLength = 5;

    private readonly ILogger<DefaultRetryPolicyFactory> _logger;
    private readonly IDatabase _redis;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisRetryPolicyFactory"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="redis"></param>
    public RedisRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger, IDatabase redis)
    {
        _logger = logger;
        _redis = redis;
    }

    /// <inheritdoc />
    public virtual async Task<AsyncRetryPolicy> CreatePolicy(string queue, long id)
    {
        var key = queue + "_m_" + id;

        var existRetry = await _redis.HashGetAsync(key, id);
        var currentRetryCount = 0;

        if (existRetry.HasValue)
        {
            currentRetryCount = (int)existRetry;
        }

        var retryCount = MaxLength - currentRetryCount;
        if (retryCount < 0)
        {
            retryCount = 0;
        }

        // 创建异步重试策略
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt =>
                {
                    var attempt = retryAttempt;
                    if (currentRetryCount != 0)
                    {
                        attempt += currentRetryCount;
                    }

                    return TimeSpan.FromSeconds(Math.Pow(2, attempt));
                },
                onRetry: async (Exception exception, TimeSpan timeSpan, int retryCount, Context context) =>
                {
                    await FaildAsync(key, id, exception, timeSpan, retryCount, context);
                });
        return retryPolicy;
    }

    /// <summary>
    /// Record retry count.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="id"></param>
    /// <param name="ex"></param>
    /// <param name="timeSpan"></param>
    /// <param name="retryCount"></param>
    /// <param name="context"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual async Task FaildAsync(string key, long id, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
    {
        await _redis.StringSetAsync(key, retryCount, TimeSpan.FromSeconds(5 * 60));
    }
}
