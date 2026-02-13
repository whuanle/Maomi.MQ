using Maomi.MQ;
using Maomi.MQ.Consumer;

namespace Maomi.MQ.RabbitMQ.UnitTests.Consumer;

public class ConsumerOptionsTests
{
    [Fact]
    public void Clone_ShouldCopyValues()
    {
        var source = new ConsumerOptions
        {
            Queue = "queue-a",
            DeadExchange = "dead-ex",
            DeadRoutingKey = "dead-route",
            Qos = 9,
            RetryFaildRequeue = true,
            Expiration = 11,
            AutoQueueDeclare = AutoQueueDeclare.Enable,
            BindExchange = "bind-ex",
            ExchangeType = ExchangeType.Direct,
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
    }

    [Fact]
    public void CopyFrom_ShouldCopyValues()
    {
        var source = new ConsumerOptions
        {
            Queue = "queue-a",
            DeadExchange = "dead-ex",
            DeadRoutingKey = "dead-route",
            Qos = 17,
            RetryFaildRequeue = false,
            Expiration = 111,
            AutoQueueDeclare = AutoQueueDeclare.Disable,
            BindExchange = "bind-ex",
            ExchangeType = ExchangeType.Topic,
            RoutingKey = "route-a",
            IsBroadcast = true,
        };

        var target = new ConsumerOptions();
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

        Assert.Null(target.IsBroadcast);
    }

    [Fact]
    public void Equals_ShouldCompareQueueExchangeTypeAndRouting()
    {
        var a = new ConsumerOptions { Queue = "q", BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };
        var b = new ConsumerOptions { Queue = "q", BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };
        var c = new ConsumerOptions { Queue = "q2", BindExchange = "e", ExchangeType = ExchangeType.Direct, RoutingKey = "r" };

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
        Assert.False(a.Equals(null));
    }
}
