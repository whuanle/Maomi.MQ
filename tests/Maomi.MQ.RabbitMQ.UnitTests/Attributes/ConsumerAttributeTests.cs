using Maomi.MQ;
using Maomi.MQ.Attributes;

namespace Maomi.MQ.RabbitMQ.UnitTests.Attributes;

public class ConsumerAttributeTests
{
    [Fact]
    public void Constructor_WithQueue_ShouldSetQueueAndDefaults()
    {
        var attribute = new ConsumerAttribute("queue-a");

        Assert.Equal("queue-a", attribute.Queue);
        Assert.Equal((ushort)100, attribute.Qos);
        Assert.True(attribute.RetryFaildRequeue);
        Assert.False(attribute.IsBroadcast);
    }

    [Fact]
    public void Constructor_WithEmptyQueue_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new ConsumerAttribute(string.Empty));
    }

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        var source = new ConsumerAttribute("queue-a")
        {
            DeadExchange = "dead-ex",
            DeadRoutingKey = "dead-key",
            Qos = 11,
            RetryFaildRequeue = false,
            Expiration = 99,
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            BindExchange = "bind-ex",
            ExchangeType = ExchangeType.Topic,
            RoutingKey = "route-a",
            IsBroadcast = true,
        };

        var clone = source.Clone();

        Assert.NotSame(source, clone);
        Assert.Equal(source.Queue, clone.Queue);
        Assert.Equal(source.DeadExchange, clone.DeadExchange);
        Assert.Equal(source.DeadRoutingKey, clone.DeadRoutingKey);
        Assert.Equal(source.Qos, clone.Qos);
        Assert.Equal(source.RetryFaildRequeue, clone.RetryFaildRequeue);
        Assert.Equal(source.Expiration, clone.Expiration);
        Assert.Equal(source.AutoQueueDeclare, clone.AutoQueueDeclare);
        Assert.Equal(source.BindExchange, clone.BindExchange);
        Assert.Equal(source.ExchangeType, clone.ExchangeType);
        Assert.Equal(source.RoutingKey, clone.RoutingKey);
        Assert.Equal(source.IsBroadcast, clone.IsBroadcast);
    }

    [Fact]
    public void CopyFrom_ShouldCopyAllFields()
    {
        var source = new ConsumerAttribute("source")
        {
            DeadExchange = "dead-ex",
            DeadRoutingKey = "dead-key",
            Qos = 22,
            RetryFaildRequeue = false,
            Expiration = 123,
            AutoQueueDeclare = AutoQueueDeclare.Disable,
            BindExchange = "bind-ex",
            ExchangeType = ExchangeType.Headers,
            RoutingKey = "route-a",
            IsBroadcast = true,
        };

        var target = new ConsumerAttribute("target");
        target.CopyFrom(source);

        Assert.Equal(source.Queue, target.Queue);
        Assert.Equal(source.DeadExchange, target.DeadExchange);
        Assert.Equal(source.DeadRoutingKey, target.DeadRoutingKey);
        Assert.Equal(source.Qos, target.Qos);
        Assert.Equal(source.RetryFaildRequeue, target.RetryFaildRequeue);
        Assert.Equal(source.Expiration, target.Expiration);
        Assert.Equal(source.AutoQueueDeclare, target.AutoQueueDeclare);
        Assert.Equal(source.BindExchange, target.BindExchange);
        Assert.Equal(source.ExchangeType, target.ExchangeType);
        Assert.Equal(source.RoutingKey, target.RoutingKey);
        Assert.Equal(source.IsBroadcast, target.IsBroadcast);
    }

    [Fact]
    public void Equals_ShouldCompareQueueExchangeTypeAndRouting()
    {
        var a = new ConsumerAttribute("q") { BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };
        var b = new ConsumerAttribute("q") { BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };
        var c = new ConsumerAttribute("q2") { BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals(null));
    }
}
