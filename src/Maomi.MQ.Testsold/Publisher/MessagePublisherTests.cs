using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using Maomi.MQ.Diagnostics;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.Tests.Publish;

public class MessagePublisherTests : BaseMock
{
    [Fact]
    public async Task Publisher()
    {
        var queue = "test";
        var idgen = new DefaultIdFactory(0);
        var eventId = idgen.NextId();

        var services = new ServiceCollection();
        var options = new MqOptions
        {
            AppName = "test",
            ConnectionFactory = _mockConnectionFactory.Object
        };
        var jsonSerializer = new DefaultMessageSerializer();
        var pool = new ConnectionPool(options);

        DefaultMessagePublisher publisher = new(options, jsonSerializer, pool, idgen, new NullLogger<DefaultMessagePublisher>());

        var eventBody = Heler.CreateEvent(eventId, queue, new TestEvent { Id = 1 });

        _mockChannel
            .Setup(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));

        await publisher.PublishAsync(queue, eventBody.Body, _ => { });
        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        var headers = new Dictionary<string, object?>();
        var basicProperties = new BasicProperties()
        {
            Headers = headers
        };

        await publisher.PublishAsync(queue, eventBody.Body, basicProperties);
        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(queue, headers[DiagnosticName.Event.Publisher]!);

        headers = new Dictionary<string, object?>();
        basicProperties = new BasicProperties()
        {
            Headers = headers
        };
        await publisher.CustomPublishAsync(queue, eventBody, basicProperties);
        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.Equal(queue, headers[DiagnosticName.Event.Publisher]!);
    }

    private class TestEvent
    {
        public int Id { get; set; }
    }
}
