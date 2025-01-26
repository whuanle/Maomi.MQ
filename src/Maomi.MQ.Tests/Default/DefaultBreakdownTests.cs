using AutoFixture;
using Maomi.MQ.Default;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Tests;

public class DefaultBreakdownTests
{
    [Fact]
    public void BasicReturn_ShouldCompleteTask()
    {
        var breakdown = new DefaultBreakdown();
        var eventArgs = new Mock<BasicReturnEventArgs>(
            It.IsAny<ushort>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IReadOnlyBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()
            );

        var result = breakdown.BasicReturn(this, eventArgs.Object);

        Assert.Equal(Task.CompletedTask, result);
    }

    [Fact]
    public void NotFoundConsumer_ShouldCompleteTask()
    {
        var breakdown = new DefaultBreakdown();
        var result =  breakdown.NotFoundConsumer(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<Type>());

        Assert.Equal(Task.CompletedTask, result);
    }
}
