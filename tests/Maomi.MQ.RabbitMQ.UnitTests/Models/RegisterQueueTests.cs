using Maomi.MQ.Consumer;
using Maomi.MQ.Models;

namespace Maomi.MQ.RabbitMQ.UnitTests.Models;

public class RegisterQueueTests
{
    [Fact]
    public void Constructor_ShouldStoreValues()
    {
        var options = new ConsumerOptions { Queue = "queue-a" };
        var registerQueue = new RegisterQueue(true, options);

        Assert.True(registerQueue.IsRegister);
        Assert.Same(options, registerQueue.Options);
        Assert.True(registerQueue.Item1);
        Assert.Same(options, registerQueue.Item2);
    }
}
