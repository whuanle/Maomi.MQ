using Maomi.MQ.Default;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace Maomi.MQ.Tests;

public class DefaultRetryPolicyFactoryTests
{
    private readonly Mock<ILogger<DefaultRetryPolicyFactory>> _loggerMock;
    private readonly DefaultRetryPolicyFactory _retryPolicyFactory;

    public DefaultRetryPolicyFactoryTests()
    {
        _loggerMock = new Mock<ILogger<DefaultRetryPolicyFactory>>();
        _retryPolicyFactory = new DefaultRetryPolicyFactory(_loggerMock.Object);
    }

    [Fact]
    public async Task CreatePolicy_ShouldReturnRetryPolicy()
    {
        string queue = "testQueue";
        string id = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

        AsyncRetryPolicy retryPolicy = await _retryPolicyFactory.CreatePolicy(queue, id);

        Assert.NotNull(retryPolicy);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnException()
    {
        string queue = "testQueue";
        string id = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

        AsyncRetryPolicy retryPolicy = await _retryPolicyFactory.CreatePolicy(queue, id);
        int retryCount = 0;

        await retryPolicy.ExecuteAsync(async () =>
        {
            retryCount++;
            if (retryCount < 3)
            {
                throw new Exception("Test exception");
            }
            await Task.CompletedTask;
        });

        Assert.Equal(3, retryCount);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnFallback()
    {
        string queue = "testQueue";
        string id = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

        int retryCount = 0;
        int fallbackCount = 0;

        DefaultRetryPolicyFactory retryPolicyFactory = new MockDefaultRetryPolicyFactory(_loggerMock.Object);
        AsyncRetryPolicy retryPolicy = await retryPolicyFactory.CreatePolicy(queue, id);
        var fallbackPolicy = Policy
            .Handle<Exception>()
            .FallbackAsync(async (c) =>
            {
                await Task.CompletedTask;
                fallbackCount = 1;
            });

        var policyWrap = fallbackPolicy.WrapAsync(retryPolicy);
        await policyWrap.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            retryCount++;
            throw new Exception("Test exception");
        });

        Assert.Equal(4, retryCount);
        Assert.Equal(1, fallbackCount);
    }

    private class MockDefaultRetryPolicyFactory : DefaultRetryPolicyFactory
    {
        public MockDefaultRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger) : base(logger)
        {
            var fieldInfo = typeof(DefaultRetryPolicyFactory).GetField("RetryBaseDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo!.SetValue(this, 1);
        }
    }
}
