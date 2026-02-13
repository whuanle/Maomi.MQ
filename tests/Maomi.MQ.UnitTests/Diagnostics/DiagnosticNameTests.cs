using Maomi.MQ.Diagnostics;

namespace Maomi.MQ.UnitTests.Diagnostics;

public class DiagnosticNameTests
{
    [Fact]
    public void RootNames_ShouldMatchExpectedConstants()
    {
        Assert.Equal("Maomi.MQ", DiagnosticName.MaomiMQ);
        Assert.Equal("Maomi.MQ.EventBus", DiagnosticName.EventBus);
        Assert.Equal("Maomi.MQ.Consumer", DiagnosticName.Consumer);
        Assert.Equal("Maomi.MQ.Publisher", DiagnosticName.Publisher);
    }

    [Fact]
    public void ListenerNames_ShouldBeStable()
    {
        Assert.Equal("MaomiMQPublisherHandlerDiagnosticListener", DiagnosticName.Listener.Publisher);
        Assert.Equal("MaomiMQConsumerHandlerDiagnosticListener", DiagnosticName.Listener.Consumer);
    }

    [Fact]
    public void ActivitySourceNames_ShouldBeStable()
    {
        Assert.Equal("Maomi.MQ.Publisher", DiagnosticName.ActivitySource.Publisher);
        Assert.Equal("Maomi.MQ.Consumer", DiagnosticName.ActivitySource.Consumer);
        Assert.Equal("Maomi.MQ.Fallback", DiagnosticName.ActivitySource.Fallback);
        Assert.Equal("Maomi.MQ.Execute", DiagnosticName.ActivitySource.Execute);
        Assert.Equal("Maomi.MQ.Retry", DiagnosticName.ActivitySource.Retry);
        Assert.Equal("Maomi.MQ.EventBus.Execute", DiagnosticName.ActivitySource.EventBusExecute);
    }

    [Fact]
    public void MeterNames_ShouldBeStable()
    {
        Assert.Equal("Maomi.MQ.Publisher", DiagnosticName.Meter.Publisher);
        Assert.Equal("Maomi.MQ.Consumer", DiagnosticName.Meter.Consumer);
        Assert.Equal("maomimq_publisher_message_count", DiagnosticName.Meter.PublisherMessageCount);
        Assert.Equal("maomimq_publisher_message_sent", DiagnosticName.Meter.PublisherMessageSent);
        Assert.Equal("maomimq_publisher_message_faild_count", DiagnosticName.Meter.PublisherFaildMessageCount);
    }

    [Fact]
    public void EventNames_ShouldBeComposedCorrectly()
    {
        Assert.Equal($"{DiagnosticName.ActivitySource.Publisher}.Start", DiagnosticName.Event.PublisherStart);
        Assert.Equal($"{DiagnosticName.ActivitySource.Publisher}.Stop", DiagnosticName.Event.PublisherStop);
        Assert.Equal($"{DiagnosticName.ActivitySource.Publisher}.Execption", DiagnosticName.Event.PublisherExecption);

        Assert.Equal($"{DiagnosticName.ActivitySource.Consumer}.Start", DiagnosticName.Event.ConsumerStart);
        Assert.Equal($"{DiagnosticName.ActivitySource.Consumer}.Stop", DiagnosticName.Event.ConsumerStop);
        Assert.Equal($"{DiagnosticName.ActivitySource.Consumer}.Execption", DiagnosticName.Event.ConsumerExecption);

        Assert.Equal("Maomi.MQ.Fallback.Start", DiagnosticName.Event.FallbackStart);
        Assert.Equal("Maomi.MQ.Fallback.Stop", DiagnosticName.Event.FallbackStop);
        Assert.Equal("Maomi.MQ.Fallback.Execption", DiagnosticName.Event.FallbackExecption);
    }
}
