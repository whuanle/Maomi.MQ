using Maomi.MQ;
using Polly;
using Polly.Retry;

namespace ActivitySourceApi;

public class MyDefaultRetryPolicyFactory : IRetryPolicyFactory
{
    private readonly ILogger<MyDefaultRetryPolicyFactory> _logger;

    public MyDefaultRetryPolicyFactory(ILogger<MyDefaultRetryPolicyFactory> logger)
    {
        _logger = logger;
    }

    public Task<AsyncRetryPolicy> CreatePolicy(string queue, string id)
    {
        // Create a retry policy.
        // 创建重试策略.
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(500),
                onRetry: async (Exception exception, TimeSpan timeSpan, int retryCount, Context context) =>
                {
                    _logger.LogDebug("Retry execution event,queue [{Queue}],retry count [{RetryCount}],timespan [{TimeSpan}]", queue, retryCount, timeSpan);
                });

        return Task.FromResult(retryPolicy);
    }
}