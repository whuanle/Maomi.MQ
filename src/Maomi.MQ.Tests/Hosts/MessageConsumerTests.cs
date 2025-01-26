//using Maomi.MQ.Default;
//using Maomi.MQ.Diagnostics;
//using Maomi.MQ.Hosts;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Polly.Retry;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using System.Diagnostics;
//using System.Diagnostics.Metrics;
//using Xunit;

//namespace Maomi.MQ.RabbitMQ.Tests.Hosts
//{
//    public class MessageConsumerTests
//    {
//        private readonly Mock<IServiceProvider> _serviceProviderMock;
//        private readonly Mock<IConsumerOptions> _consumerOptionsMock;
//        private readonly Mock<Func<IServiceProvider, object>> _consumerInstanceMock;
//        private readonly Mock<IMessageSerializer> _messageSerializerMock;
//        private readonly Mock<IRetryPolicyFactory> _policyFactoryMock;
//        private readonly Mock<ILogger> _loggerMock;
//        private readonly Mock<IChannel> _channelMock;
//        private readonly Mock<IConsumer<TestMessage>> _consumerMock;
//        private readonly Mock<IBreakdown> _breakdownMock;
//        private readonly Mock<IMeterFactory> _meterFactoryMock;
//        private readonly Mock<Meter> _meterMock;
//        private readonly Mock<Counter<int>> _counterMock;
//        private readonly Mock<Histogram<long>> _histogramMock;

//        public MessageConsumerTests()
//        {
//            _serviceProviderMock = new Mock<IServiceProvider>();
//            _consumerOptionsMock = new Mock<IConsumerOptions>();
//            _consumerInstanceMock = new Mock<Func<IServiceProvider, object>>();
//            _messageSerializerMock = new Mock<IMessageSerializer>();
//            _policyFactoryMock = new Mock<IRetryPolicyFactory>();
//            _loggerMock = new Mock<ILogger>();
//            _channelMock = new Mock<IChannel>();
//            _consumerMock = new Mock<IConsumer<TestMessage>>();
//            _breakdownMock = new Mock<IBreakdown>();
//            _meterFactoryMock = new Mock<IMeterFactory>();
//            _meterMock = new Mock<Meter>();
//            _counterMock = new Mock<Counter<int>>();
//            _histogramMock = new Mock<Histogram<long>>();

//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(ServiceFactory))).Returns(new ServiceFactory());
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(Mock.Of<ILoggerFactory>());
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IBreakdown))).Returns(_breakdownMock.Object);
//            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMeterFactory))).Returns(_meterFactoryMock.Object);

//            _meterFactoryMock.Setup(mf => mf.Create(It.IsAny<string>())).Returns(_meterMock.Object);
//            _meterMock.Setup(m => m.CreateCounter<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TagList>())).Returns(_counterMock.Object);
//            _meterMock.Setup(m => m.CreateHistogram<long>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TagList>())).Returns(_histogramMock.Object);
//        }

//        [Fact]
//        public async Task ConsumerAsync_Should_Ack_When_Success()
//        {
//            // Arrange
//            var messageConsumer = new MessageConsumer(_serviceProviderMock.Object, _consumerOptionsMock.Object, _consumerInstanceMock.Object);
//            var eventArgs = new BasicDeliverEventArgs { Body = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }) };
//            var messageHeader = new MessageHeader();
//            _consumerInstanceMock.Setup(ci => ci(It.IsAny<IServiceProvider>())).Returns(_consumerMock.Object);
//            _messageSerializerMock.Setup(ms => ms.Deserialize<TestMessage>(It.IsAny<ReadOnlySpan<byte>>())).Returns(new TestMessage());
//            _policyFactoryMock.Setup(pf => pf.CreatePolicy(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Policy.NoOpAsync<ConsumerState>());

//            // Act
//            await messageConsumer.ConsumerAsync<TestMessage>(_channelMock.Object, eventArgs);

//            // Assert
//            _channelMock.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), false), Times.Once);
//        }

//        [Fact]
//        public async Task ConsumerAsync_Should_Nack_When_Exception()
//        {
//            // Arrange
//            var messageConsumer = new MessageConsumer(_serviceProviderMock.Object, _consumerOptionsMock.Object, _consumerInstanceMock.Object);
//            var eventArgs = new BasicDeliverEventArgs { Body = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }) };
//            var messageHeader = new MessageHeader();
//            _consumerInstanceMock.Setup(ci => ci(It.IsAny<IServiceProvider>())).Returns(_consumerMock.Object);
//            _messageSerializerMock.Setup(ms => ms.Deserialize<TestMessage>(It.IsAny<ReadOnlySpan<byte>>())).Throws(new Exception());
//            _policyFactoryMock.Setup(pf => pf.CreatePolicy(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Policy.NoOpAsync<ConsumerState>());

//            // Act
//            await messageConsumer.ConsumerAsync<TestMessage>(_channelMock.Object, eventArgs);

//            // Assert
//            _channelMock.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), false, It.IsAny<bool>()), Times.Once);
//        }

//        private class TestMessage { }
//    }
//}
