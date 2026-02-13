using Maomi.MQ;

namespace Maomi.MQ.UnitTests.Core;

public class ConsumerStateTests
{
    [Fact]
    public void ConsumerState_ShouldKeepExpectedNumericValues()
    {
        Assert.Equal(1, (int)ConsumerState.Ack);
        Assert.Equal(2, (int)ConsumerState.Nack);
        Assert.Equal(4, (int)ConsumerState.NackAndRequeue);
        Assert.Equal(8, (int)ConsumerState.NackAndNoRequeue);
        Assert.Equal(16, (int)ConsumerState.Exception);
    }

    [Fact]
    public void ConsumerState_ShouldBeDistinctBitFlagsExceptAck()
    {
        var states = new[]
        {
            ConsumerState.Nack,
            ConsumerState.NackAndRequeue,
            ConsumerState.NackAndNoRequeue,
            ConsumerState.Exception,
        };

        foreach (var state in states)
        {
            var value = (int)state;
            Assert.True((value & (value - 1)) == 0);
        }
    }
}
