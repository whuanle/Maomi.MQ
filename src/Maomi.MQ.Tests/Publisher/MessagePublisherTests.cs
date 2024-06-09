//using IdGen;
//using Maomi.MQ.Defaults;
//using Maomi.MQ.Pool;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging.Abstractions;
//using Moq;
//using RabbitMQ.Client;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Maomi.MQ.Tests.Publish;
//public class MessagePublisherTests
//{
//    private readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
//    private readonly Mock<IConnection> _mockConnection = new Mock<IConnection>();
//    private readonly Mock<IChannel> _mockChannel = new Mock<IChannel>();

//    public MessagePublisherTests()
//    {
//        _mockConnectionFactory
//            .Setup(c => c.CreateConnectionAsync(CancellationToken.None))
//            .Returns(Task.FromResult(_mockConnection.Object));
//        _mockConnection
//            .Setup(c => c.CreateChannelAsync(CancellationToken.None))
//            .Returns(Task.FromResult(_mockChannel.Object));
//    }

//    [Fact]
//    public async Task Publisher()
//    {
//        var services = new ServiceCollection();
//        var options = new DefaultMqOptions
//        {
//            ConnectionFactory = _mockConnectionFactory.Object
//        };
//        var jsonSerializer = new DefaultJsonSerializer();
//        var pool = new ConnectionPool(new ConnectionPooledObjectPolicy(options));
//        var idgen = new IdGenerator(0, IdGeneratorOptions.Default);
//        DefaultMessagePublisher publisher = new(options, jsonSerializer, pool, idgen,new NullLogger<DefaultMessagePublisher>());

//        var eventBody = new EventBody<TestEvent1>()
//        {
//            Id = 1,
//            Queue = "test",
//            CreationTime = DateTimeOffset.Now,
//            Body = new TestEvent1
//            {
//                Id = 1
//            }
//        };

//        _mockChannel
//            .Setup(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));

//        await publisher.PublishAsync("test", eventBody.Body, _ => { });
//        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

//        await publisher.PublishAsync("test", eventBody.Body, new BasicProperties());
//        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

//        await publisher.PublishAsync("test", eventBody, new BasicProperties());
//        _mockChannel.Verify(c => c.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

//    }

//    public class TestEvent1
//    {
//        public int Id { get; set; }
//    }
//}
