using Polly;
using Polly.Retry;

namespace Maomi.MQ.Retry
{
    public class DefaultPolicyFactory : IPolicyFactory
    {
        public AsyncRetryPolicy CreatePolicy(string queue)
        {
            // 创建异步重试策略
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, retryCount, context) =>
                    {

                    });
            return retryPolicy;
        }

        public Task FaildAsync(string queue)
        {
            return Task.CompletedTask;
        }
    }
}
