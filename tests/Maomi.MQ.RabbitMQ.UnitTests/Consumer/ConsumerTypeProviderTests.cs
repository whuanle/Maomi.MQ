using Maomi.MQ.Consumer;

namespace Maomi.MQ.RabbitMQ.UnitTests.Consumer;

public class ConsumerTypeProviderTests
{
    [Fact]
    public void Constructor_WithList_ShouldExposeSameItems()
    {
        var items = new List<ConsumerType>
        {
            new() { Queue = "q1" },
            new() { Queue = "q2" },
        };

        var provider = new ConsumerTypeProvider(items);

        Assert.Equal(2, provider.Consumers.Count);
        Assert.Contains(provider.Consumers, x => x.Queue == "q1");
        Assert.Contains(provider.Consumers, x => x.Queue == "q2");
    }

    [Fact]
    public void Constructor_Empty_ShouldStartEmpty()
    {
        var provider = new ConsumerTypeProvider();
        Assert.Empty(provider.Consumers);
    }
}
