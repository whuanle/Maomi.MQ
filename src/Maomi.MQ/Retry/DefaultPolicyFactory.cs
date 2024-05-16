using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Maomi.MQ.Retry
{
    /// <summary>
    /// 默认的策略提供器.
    /// </summary>
    public class DefaultPolicyFactory : IPolicyFactory
    {
        private readonly ILogger<DefaultPolicyFactory> _logger;

        public DefaultPolicyFactory(ILogger<DefaultPolicyFactory> logger)
        {
            _logger = logger;
        }

        public virtual Task<AsyncRetryPolicy> CreatePolicy(string queue)
        {
            // 创建异步重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: async (Exception exception, TimeSpan timeSpan, int retryCount, Context context) =>
                    {
                        _logger.LogDebug("重试");
                        await FaildAsync(queue, exception, timeSpan, retryCount, context);
                    });
            return Task.FromResult(retryPolicy);
        }

        // 每次失败重试，重新放到 redis
        public virtual Task FaildAsync(string queue, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
        {
            return Task.CompletedTask;
        }
    }
}
