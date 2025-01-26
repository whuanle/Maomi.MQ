using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maomi.MQ.Tests;

public class EventBusConsumerTests
{
    private readonly Mock<IEventMiddleware<TestMessage>> _eventMiddlewareMock;
    private readonly Mock<IHandlerMediator<TestMessage>> _handlerMediatorMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly EventBusConsumer<TestMessage> _eventBusConsumer;

    public EventBusConsumerTests()
    {
        _eventMiddlewareMock = new Mock<IEventMiddleware<TestMessage>>();
        _handlerMediatorMock = new Mock<IHandlerMediator<TestMessage>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        _eventBusConsumer = new EventBusConsumer<TestMessage>(_eventMiddlewareMock.Object, _handlerMediatorMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Call_EventMiddleware_ExecuteAsync()
    {
        var messageHeader = new MessageHeader();
        var message = new TestMessage();

        await _eventBusConsumer.ExecuteAsync(messageHeader, message);

        _eventMiddlewareMock.Verify(em => em.ExecuteAsync(messageHeader, message, _handlerMediatorMock.Object.ExecuteAsync), Times.Once);
    }

    [Fact]
    public async Task FaildAsync_Should_Call_EventMiddleware_FaildAsync()
    {
        var messageHeader = new MessageHeader();
        var exception = new Exception();
        var retryCount = 1;
        var message = new TestMessage();

        await _eventBusConsumer.FaildAsync(messageHeader, exception, retryCount, message);

        _eventMiddlewareMock.Verify(em => em.FaildAsync(messageHeader, exception, retryCount, message), Times.Once);
    }

    [Fact]
    public async Task FallbackAsync_Should_Call_EventMiddleware_FallbackAsync()
    {
        var messageHeader = new MessageHeader();
        var message = new TestMessage();
        var exception = new Exception();

        await _eventBusConsumer.FallbackAsync(messageHeader, message, exception);

        _eventMiddlewareMock.Verify(em => em.FallbackAsync(messageHeader, message, exception), Times.Once);
    }

    public class TestMessage
    {
    }
}
