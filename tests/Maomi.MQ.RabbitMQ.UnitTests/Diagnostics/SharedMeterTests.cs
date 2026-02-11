using Maomi.MQ.Diagnostics;

namespace Maomi.MQ.RabbitMQ.UnitTests.Diagnostics;

public class SharedMeterTests
{
    [Fact]
    public void PublisherAndConsumer_ShouldExposeNamedMeters()
    {
        Assert.NotNull(SharedMeter.Publisher);
        Assert.NotNull(SharedMeter.Consumer);

        Assert.Equal(DiagnosticName.Meter.Publisher, SharedMeter.Publisher.Name);
        Assert.Equal(DiagnosticName.Meter.Consumer, SharedMeter.Consumer.Name);
    }
}
