using Maomi.MQ.Default;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.RabbitMQ.UnitTests.Default;

public class DefaultBreakdownTests
{
    [Fact]
    public async Task BasicReturnAsync_ShouldComplete()
    {
        var logger = new Mock<ILogger<DefaultBreakdown>>();
        var breakdown = new DefaultBreakdown(logger.Object);

        var args = new BasicReturnEventArgs(
            replyCode: 312,
            replyText: "NO_ROUTE",
            exchange: "ex",
            routingKey: "route",
            basicProperties: new BasicProperties(),
            body: ReadOnlyMemory<byte>.Empty,
            cancellationToken: CancellationToken.None);

        await breakdown.BasicReturnAsync(this, args);
    }

    [Fact]
    public async Task NotFoundConsumerAsync_ShouldComplete()
    {
        var logger = new Mock<ILogger<DefaultBreakdown>>();
        var breakdown = new DefaultBreakdown(logger.Object);

        await breakdown.NotFoundConsumerAsync("queue-a", typeof(string), typeof(int));
    }
}
