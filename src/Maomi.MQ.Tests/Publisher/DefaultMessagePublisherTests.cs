//using Maomi.MQ.Diagnostics;
//using Maomi.MQ.Pool;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using RabbitMQ.Client;
//using System.Diagnostics;
//using System.Diagnostics.Metrics;
//using Xunit;

//namespace Maomi.MQ.Tests
//{
//    public class DefaultMessagePublisherTests
//    {
//        private readonly Mock<IServiceProvider> _serviceProviderMock;
//        private readonly Mock<MqOptions> _mqOptionsMock;
//        private readonly Mock<IMessageSerializer> _messageSerializerMock;
//        private readonly Mock<ConnectionPool> _connectionPoolMock;
//        private readonly Mock<ConnectionObject> _connectionObjectMock;
//        private readonly Mock<IIdFactory> _idGenMock;
//        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
//        private readonly Mock<ILogger> _loggerMock;
//        private readonly Mock<IConsumerTypeProvider> _consumerTypeProviderMock;
//        private readonly Mock<IRoutingProvider> _routingProviderMock;
//        private readonly Mock<IMeterFactory> _meterFactoryMock;
//        private readonly Mock<Meter> _meterMock;
//        private readonly DefaultMessagePublisher _publisher;

//        public DefaultMessagePublisherTests()
//        {
//            _serviceProviderMock = new Mock<IServiceProvider>();
//            _mqOptionsMock = new Mock<MqOptions>();
//            _messageSerializerMock = new Mock<IMessageSerializer>();
//            _connectionPoolMock = new Mock<ConnectionPool>(_mqOptionsMock.Object);
//            _connectionObjectMock = new Mock<ConnectionObject>(_mqOptionsMock.Object);
//            _idGenMock = new Mock<IIdFactory>();
//            _loggerFactoryMock = new Mock<ILoggerFactory>();
//            _loggerMock = new Mock<ILogger>();
//            _consumerTypeProviderMock = new Mock<IConsumerTypeProvider>();
//            _routingProviderMock = new Mock<IRoutingProvider>();
//            _meterFactoryMock = new Mock<IMeterFactory>();
//            _meterMock = new Mock<Meter>();

//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IConsumerTypeProvider))).Returns(_consumerTypeProviderMock.Object);
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IRoutingProvider))).Returns(_routingProviderMock.Object);
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMeterFactory))).Returns(_meterFactoryMock.Object);

//            _loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);
//            _connectionPoolMock.Setup(cp => cp.Get()).Returns(_connectionObjectMock.Object);
//            _meterFactoryMock.Setup(mf => mf.Create(It.IsAny<string>())).Returns(_meterMock.Object);

//            _publisher = new DefaultMessagePublisher(
//                _serviceProviderMock.Object,
//                _mqOptionsMock.Object,
//                _messageSerializerMock.Object,
//                _connectionPoolMock.Object,
//                _idGenMock.Object,
//                _loggerFactoryMock.Object);
//        }

//        [Fact]
//        public async Task PublishAsync_ShouldPublishMessage()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var basicProperties = new BasicProperties();
//            var consumerOptions = new Mock<IConsumerOptions>();
//            consumerOptions.Setup(co => co.BindExchange).Returns("test-exchange");
//            consumerOptions.Setup(co => co.RoutingKey).Returns("test-routing-key");

//            _consumerTypeProviderMock.Setup(ctp => ctp.First(It.IsAny<Func<ConsumerType, bool>>()))
//                .Returns(new ConsumerType { Event = message.GetType(), ConsumerOptions = consumerOptions.Object });

//            _routingProviderMock.Setup(rp => rp.Get(It.IsAny<IConsumerOptions>())).Returns(consumerOptions.Object);

//            // Act
//            await _publisher.PublishAsync(message, basicProperties);

//            // Assert
//            _connectionObjectMock.Verify(co => co.DefaultChannel.BasicPublishAsync(
//                "test-exchange",
//                "test-routing-key",
//                basicProperties,
//                It.IsAny<byte[]>(),
//                true,
//                It.IsAny<CancellationToken>()), Times.Once);
//        }

//        [Fact]
//        public async Task PublishAsync_ShouldHandleException()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var basicProperties = new BasicProperties();
//            var consumerOptions = new Mock<IConsumerOptions>();
//            consumerOptions.Setup(co => co.BindExchange).Returns("test-exchange");
//            consumerOptions.Setup(co => co.RoutingKey).Returns("test-routing-key");

//            _consumerTypeProviderMock.Setup(ctp => ctp.First(It.IsAny<Func<ConsumerType, bool>>()))
//                .Returns(new ConsumerType { Event = message.GetType(), ConsumerOptions = consumerOptions.Object });

//            _routingProviderMock.Setup(rp => rp.Get(It.IsAny<IConsumerOptions>())).Returns(consumerOptions.Object);

//            _connectionObjectMock.Setup(co => co.DefaultChannel.BasicPublishAsync(
//                It.IsAny<string>(),
//                It.IsAny<string>(),
//                It.IsAny<BasicProperties>(),
//                It.IsAny<byte[]>(),
//                It.IsAny<bool>(),
//                It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test Exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(() => _publisher.PublishAsync(message, basicProperties));
//        }

//        [Fact]
//        public async Task PublishAsync_ShouldUseDefaultProperties()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var consumerOptions = new Mock<IConsumerOptions>();
//            consumerOptions.Setup(co => co.BindExchange).Returns("test-exchange");
//            consumerOptions.Setup(co => co.RoutingKey).Returns("test-routing-key");

//            _consumerTypeProviderMock.Setup(ctp => ctp.First(It.IsAny<Func<ConsumerType, bool>>()))
//                .Returns(new ConsumerType { Event = message.GetType(), ConsumerOptions = consumerOptions.Object });

//            _routingProviderMock.Setup(rp => rp.Get(It.IsAny<IConsumerOptions>())).Returns(consumerOptions.Object);

//            // Act
//            await _publisher.PublishAsync(message);

//            // Assert
//            _connectionObjectMock.Verify(co => co.DefaultChannel.BasicPublishAsync(
//                "test-exchange",
//                "test-routing-key",
//                It.Is<BasicProperties>(bp => bp.DeliveryMode == DeliveryModes.Persistent),
//                It.IsAny<byte[]>(),
//                true,
//                It.IsAny<CancellationToken>()), Times.Once);
//        }

//        [Fact]
//        public async Task PublishAsync_WithCustomProperties_ShouldOverrideDefaultProperties()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var customProperties = new BasicProperties { DeliveryMode = DeliveryModes.NonPersistent };
//            var consumerOptions = new Mock<IConsumerOptions>();
//            consumerOptions.Setup(co => co.BindExchange).Returns("test-exchange");
//            consumerOptions.Setup(co => co.RoutingKey).Returns("test-routing-key");

//            _consumerTypeProviderMock.Setup(ctp => ctp.First(It.IsAny<Func<ConsumerType, bool>>()))
//                .Returns(new ConsumerType { Event = message.GetType(), ConsumerOptions = consumerOptions.Object });

//            _routingProviderMock.Setup(rp => rp.Get(It.IsAny<IConsumerOptions>())).Returns(consumerOptions.Object);

//            // Act
//            await _publisher.PublishAsync(message, customProperties);

//            // Assert
//            _connectionObjectMock.Verify(co => co.DefaultChannel.BasicPublishAsync(
//                "test-exchange",
//                "test-routing-key",
//                It.Is<BasicProperties>(bp => bp.DeliveryMode == DeliveryModes.NonPersistent),
//                It.IsAny<byte[]>(),
//                true,
//                It.IsAny<CancellationToken>()), Times.Once);
//        }

//        [Fact]
//        public async Task CustomPublishAsync_ShouldPublishMessage()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var basicProperties = new BasicProperties();

//            // Act
//            await _publisher.CustomPublishAsync("test-exchange", "test-routing-key", message, basicProperties);

//            // Assert
//            _connectionObjectMock.Verify(co => co.DefaultChannel.BasicPublishAsync(
//                "test-exchange",
//                "test-routing-key",
//                basicProperties,
//                It.IsAny<byte[]>(),
//                true,
//                It.IsAny<CancellationToken>()), Times.Once);
//        }

//        [Fact]
//        public async Task CustomPublishAsync_ShouldHandleException()
//        {
//            // Arrange
//            var message = new { Content = "Test Message" };
//            var basicProperties = new BasicProperties();

//            _connectionObjectMock.Setup(co => co.DefaultChannel.BasicPublishAsync(
//                It.IsAny<string>(),
//                It.IsAny<string>(),
//                It.IsAny<BasicProperties>(),
//                It.IsAny<byte[]>(),
//                It.IsAny<bool>(),
//                It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test Exception"));

//            // Act & Assert
//            await Assert.ThrowsAsync<Exception>(() => _publisher.CustomPublishAsync("test-exchange", "test-routing-key", message, basicProperties));
//        }
//    }
//}
