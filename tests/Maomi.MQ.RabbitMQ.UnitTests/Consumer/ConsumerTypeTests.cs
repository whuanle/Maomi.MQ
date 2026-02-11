using Maomi.MQ.Consumer;

namespace Maomi.MQ.RabbitMQ.UnitTests.Consumer;

public class ConsumerTypeTests
{
    [Fact]
    public void Equals_SameQueue_ShouldBeTrue()
    {
        var a = new ConsumerType { Queue = "q" };
        var b = new ConsumerType { Queue = "q" };

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentQueue_ShouldBeFalse()
    {
        var a = new ConsumerType { Queue = "q1" };
        var b = new ConsumerType { Queue = "q2" };

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void CompareTo_ShouldCompareByQueue()
    {
        var a = new ConsumerType { Queue = "a" };
        var b = new ConsumerType { Queue = "b" };

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(new ConsumerType { Queue = "a" }));
    }

    [Fact]
    public void GetHashCode_ShouldUseQueue()
    {
        var a = new ConsumerType { Queue = "q" };
        var b = new ConsumerType { Queue = "q" };

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
