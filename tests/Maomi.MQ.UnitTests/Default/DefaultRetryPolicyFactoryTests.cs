using Maomi.MQ.Default;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maomi.MQ.UnitTests.Default;

public class DefaultRetryPolicyFactoryTests
{
    [Fact]
    public async Task CreatePolicy_SameQueue_ShouldReusePolicyInstance()
    {
        var logger = new Mock<ILogger<DefaultRetryPolicyFactory>>();
        var factory = new DefaultRetryPolicyFactory(logger.Object);

        var p1 = await factory.CreatePolicy("queue-a", "1");
        var p2 = await factory.CreatePolicy("queue-a", "2");

        Assert.Same(p1, p2);
    }

    [Fact]
    public async Task CreatePolicy_DifferentQueue_ShouldCreateDifferentPolicyInstance()
    {
        var logger = new Mock<ILogger<DefaultRetryPolicyFactory>>();
        var factory = new DefaultRetryPolicyFactory(logger.Object);

        var p1 = await factory.CreatePolicy("queue-a", "1");
        var p2 = await factory.CreatePolicy("queue-b", "1");

        Assert.NotSame(p1, p2);
    }

    [Fact]
    public async Task Policy_ShouldRetryThreeTimesThenSucceed()
    {
        var logger = new Mock<ILogger<DefaultRetryPolicyFactory>>();
        var factory = new DefaultRetryPolicyFactory(logger.Object);
        var policy = await factory.CreatePolicy("queue-a", "1");
        var count = 0;

        await policy.ExecuteAsync(() =>
        {
            count++;
            if (count < 4)
            {
                throw new InvalidOperationException("retry");
            }

            return Task.CompletedTask;
        });

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task FaildAsync_ShouldCompleteWithoutThrowing()
    {
        var logger = new Mock<ILogger<DefaultRetryPolicyFactory>>();
        var factory = new DefaultRetryPolicyFactory(logger.Object);

        await factory.FaildAsync("q", new Exception("x"), TimeSpan.FromSeconds(1), 2, new Polly.Context());
    }
}
