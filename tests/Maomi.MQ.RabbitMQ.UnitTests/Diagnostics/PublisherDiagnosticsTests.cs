using Maomi.MQ;
using Maomi.MQ.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Maomi.MQ.RabbitMQ.UnitTests.Diagnostics;

public class PublisherDiagnosticsTests
{
    [Fact]
    public void StartStopExceptionRecord_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();

        var options = new MqOptions
        {
            AppName = "app",
            WorkId = 1,
            AutoQueueDeclare = true,
            ConnectionFactory = new Moq.Mock<IConnectionFactory>().Object,
            MessageSerializers = Array.Empty<IMessageSerializer>(),
        };

        var diagnostics = new PublisherDiagnostics(provider, options);

        var header = new MessageHeader
        {
            Id = "id",
            AppId = "app",
            ContentType = "application/json",
            Type = "type",
        };

        var activity = diagnostics.Start(header, "ex", "route");
        diagnostics.RecordMessageSize(header, "ex", "route", 12, activity);
        diagnostics.Exception(header, "ex", "route", new Exception("boom"), activity);
        diagnostics.Stop(header, "ex", "route", activity);
    }
}
