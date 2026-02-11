using Maomi.MQ.Consumer;
using Maomi.MQ.Models;

namespace Maomi.MQ.RabbitMQ.UnitTests.Models;

public class RoutingProviderTests
{
    [Fact]
    public void Get_ConsumerOptions_ShouldReturnSameReference()
    {
        var provider = new RoutingProvider();
        var options = new ConsumerOptions { Queue = "queue-a" };

        var result = provider.Get(options);

        Assert.Same(options, result);
    }

    [Fact]
    public void Get_QueueNameOptions_ShouldReturnSameReference()
    {
        var provider = new RoutingProvider();
        var options = new QueueNameAttribute("ex-a", "route-a");

        var result = provider.Get(options);

        Assert.Same(options, result);
    }
}
