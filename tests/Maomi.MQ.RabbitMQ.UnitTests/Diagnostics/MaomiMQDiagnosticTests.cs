namespace Maomi.MQ.RabbitMQ.UnitTests.Diagnostics;

public class MaomiMQDiagnosticTests
{
    [Fact]
    public void Sources_ShouldContainPublisherAndSubscriberSourceNames()
    {
        Assert.Contains("RabbitMQ.Client.Publisher", Maomi.MQ.MaomiMQDiagnostic.Sources);
        Assert.Contains("RabbitMQ.Client.Subscriber", Maomi.MQ.MaomiMQDiagnostic.Sources);
    }
}
