// <copyright file="DefaultRetryPolicyFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Maomi.MQ.Default;

/// <summary>
/// Default retry policy.<br />
/// 默认的策略提供器.
/// </summary>
public class DefaultRetryPolicyFactory : IRetryPolicyFactory
{
    private readonly ILogger<DefaultRetryPolicyFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRetryPolicyFactory"/> class.
    /// </summary>
    /// <param name="logger"></param>
    public DefaultRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public virtual Task<AsyncRetryPolicy> CreatePolicy(string queue, long id)
    {
        // Create a retry policy.
        // 创建重试策略.
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: async (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogDebug("Retry execution event,queue [{Queue}],retry count [{RetryCount}],timespan [{TimeSpan}]", queue, retryCount, timeSpan);
                    await FaildAsync(queue, exception, timeSpan, retryCount, context);
                });

        return Task.FromResult(retryPolicy);
    }

    /// <summary>
    /// Executed when retry fails.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="ex"></param>
    /// <param name="timeSpan"></param>
    /// <param name="retryCount"></param>
    /// <param name="context"></param>
    /// <returns><see cref="Task"/>.</returns>
    public virtual Task FaildAsync(string queue, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
    {
        return Task.CompletedTask;
    }
}
