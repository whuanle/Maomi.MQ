using Maomi.MQ.Retry;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace Maomi.MQ.RedisRetry
{
    public class RedisRetryPolicyFactory : IRetryPolicyFactory
    {
        private const int MaxLength = 5;

        private readonly ILogger<DefaultRetryPolicyFactory> _logger;
        private readonly IDatabase _redis;

        public RedisRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger, IDatabase redis)
        {
            _logger = logger;
            _redis = redis;
        }

        public virtual async Task<AsyncRetryPolicy> CreatePolicy(string queue)
        {
            var existRetry = await _redis.StringGetAsync(queue);
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
                        _logger.LogDebug("重试");
                        await FaildAsync(queue, exception, timeSpan, retryCount, context);
                    });
            return retryPolicy;
        }

        public virtual async Task<AsyncRetryPolicy> CreatePolicy(string queue, long id)
        {
            var key = queue + "_m";

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
                        _logger.LogDebug("重试");
                        await FaildAsync(queue,id, exception, timeSpan, retryCount, context);
                    });
            return retryPolicy;
        }
        // 每次失败重试，重新放到 redis
        public virtual async Task FaildAsync(string queue, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
        {
            var queueName = queue;
            long value = await _redis.StringIncrementAsync(queueName, retryCount);
        }

        // 每次失败重试，重新放到 redis
        public virtual async Task FaildAsync(string key, long id, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
        {
            await _redis.HashSetAsync(key, id, retryCount);
        }
    }
}
