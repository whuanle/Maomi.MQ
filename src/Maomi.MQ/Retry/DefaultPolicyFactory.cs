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

        public AsyncRetryPolicy CreatePolicy(string queue)
        {
            // 创建异步重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: async (Exception exception, TimeSpan retryCount, Context context) =>
                    {
                        await FaildAsync(queue, exception, retryCount, context);
                    });
            return retryPolicy;
        }

        public Task FaildAsync(string queue, Exception ex, TimeSpan retryCount, Context context)
        {
            return Task.CompletedTask;
        }
    }
}
