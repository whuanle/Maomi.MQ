using Maomi.MQ;
using Maomi.MQ.Consumer;
using Maomi.MQ.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Diagnostics;

public class ConsumerDiagnosticsTests
{
    [Fact]
    public void StartAndStopConsume_ShouldNotThrow()
    {
        var diagnostics = CreateDiagnostics();
        var header = new MessageHeader { Id = "id", AppId = "app", ContentType = "application/json", Type = "t" };
        var options = new ConsumerOptions { Queue = "queue-a", BindExchange = "ex", RoutingKey = "route" };

        var properties = new BasicProperties
        {
            MessageId = "id",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            ContentType = "application/json",
            Type = "t",
            AppId = "app",
        };

        var args = new BasicDeliverEventArgs(
            "ctag",
            1,
            false,
            "ex",
            "route",
            properties,
            ReadOnlyMemory<byte>.Empty,
            CancellationToken.None);

        var activity = diagnostics.StartConsume(header, args, options);
        diagnostics.StopConsume(header, activity);
    }

    [Fact]
    public void StartStopExecuteRetryFallbackAndException_ShouldNotThrow()
    {
        var diagnostics = CreateDiagnostics();
        var header = new MessageHeader { Id = "id", AppId = "app", ContentType = "application/json", Type = "t" };

        var execute = diagnostics.StartExecute(header);
        diagnostics.ExceptionExecute(header, new Exception("boom"), execute);
        diagnostics.StopExecute(header, execute);

        var retry = diagnostics.StartRetry(header);
        diagnostics.ExceptionRetry(header, new Exception("boom"), retry);
        diagnostics.StopRetry(header, retry);

        var fallback = diagnostics.StartFallback(header);
        diagnostics.ExceptionFallback(header, new Exception("boom"), fallback);
        diagnostics.StopFallback(header, ConsumerState.Nack, fallback);
    }

    [Fact]
    public void RecordFail_ShouldNotThrow()
    {
        var diagnostics = CreateDiagnostics();
        var header = new MessageHeader { Id = "id", AppId = "app", ContentType = "application/json", Type = "t" };
        var options = new ConsumerOptions { Queue = "queue-a", BindExchange = "ex", RoutingKey = "route" };

        diagnostics.RecordFail(header, options);
    }

    private static ConsumerDiagnostics CreateDiagnostics()
    {
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        return new ConsumerDiagnostics(provider);
    }
}
