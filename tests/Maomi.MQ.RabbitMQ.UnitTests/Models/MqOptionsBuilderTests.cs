using Maomi.MQ.Models;

namespace Maomi.MQ.RabbitMQ.UnitTests.Models;

public class MqOptionsBuilderTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var builder = new MqOptionsBuilder();

        Assert.False(string.IsNullOrWhiteSpace(builder.AppName));
        Assert.Equal(0, builder.WorkId);
        Assert.True(builder.AutoQueueDeclare);
        Assert.Null(builder.MessageSerializers);
    }
}
