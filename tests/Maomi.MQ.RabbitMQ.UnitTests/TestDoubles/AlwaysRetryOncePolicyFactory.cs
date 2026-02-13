using Moq;
using Polly;
using Polly.Retry;

namespace Maomi.MQ.RabbitMQ.UnitTests.TestDoubles;

internal sealed class AlwaysRetryOncePolicyFactory : IRetryPolicyFactory
{
    public Task<AsyncRetryPolicy> CreatePolicy(string queue, string id)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 1,
                sleepDurationProvider: _ => TimeSpan.Zero,
                onRetryAsync: (exception, span, retryCount, context) => Task.CompletedTask);

        return Task.FromResult(policy);
    }
}
