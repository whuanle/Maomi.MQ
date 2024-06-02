using Maomi.MQ.Retry;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Maomi.MQ.Tests;

public class TestRetryPolicyFactory : DefaultRetryPolicyFactory
{
    public int RetryCount { get; private set; }
    public TestRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger) : base(logger)
    {
    }

    public override Task<AsyncRetryPolicy> CreatePolicy(string queue, long id)
    {
        // Create a retry policy.
        // 创建重试策略.
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100),
                onRetry: async (exception, timeSpan, retryCount, context) =>
                {
                    await FaildAsync(queue, exception, timeSpan, retryCount, context);
                });

        return Task.FromResult(retryPolicy);
    }

    public override Task FaildAsync(string queue, Exception ex, TimeSpan timeSpan, int retryCount, Context context)
    {
        RetryCount++;
        return Task.CompletedTask;
    }
}