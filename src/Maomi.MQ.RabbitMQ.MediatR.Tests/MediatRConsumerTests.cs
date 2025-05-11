using Maomi.MQ.EventBus;
using Maomi.MQ.MediatR;
using MediatR;
using Moq;
using Xunit;

public class MediatRConsumerTests
{
    private readonly Mock<IEventMiddleware<IRequest>> _eventMiddlewareMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly MediatrConsumer<IRequest> _consumer;

    public MediatRConsumerTests()
    {
        _eventMiddlewareMock = new Mock<IEventMiddleware<IRequest>>();
        _mediatorMock = new Mock<IMediator>();
        _consumer = new MediatrConsumer<IRequest>(_eventMiddlewareMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Invoke_Mediator_Send()
    {
        // Arrange
        var messageHeader = new MessageHeader();
        var message = Mock.Of<IRequest>();
        EventHandlerDelegate<IRequest> capturedDelegate = null;

        _eventMiddlewareMock
            .Setup(m => m.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<IRequest>>()))
            .Callback<MessageHeader, IRequest, EventHandlerDelegate<IRequest>>((_, _, next) => capturedDelegate = next)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.ExecuteAsync(messageHeader, message);

        // Assert
        Assert.NotNull(capturedDelegate);
        await capturedDelegate.Invoke(messageHeader, message, default);
        _mediatorMock.Verify(m => m.Send(message, default), Times.Once);
    }

    [Fact]
    public async Task FaildAsync_Should_Invoke_EventMiddleware_FallbackAsync()
    {
        // Arrange
        var messageHeader = new MessageHeader();
        var exception = new Exception("Test exception");
        var retryCount = 3;
        var message = Mock.Of<IRequest>();

        // Act
        await _consumer.FaildAsync(messageHeader, exception, retryCount, message);

        // Assert
        _eventMiddlewareMock.Verify(m => m.FallbackAsync(messageHeader, message, exception), Times.Once);
    }

    [Fact]
    public async Task FallbackAsync_Should_Invoke_EventMiddleware_FallbackAsync()
    {
        // Arrange
        var messageHeader = new MessageHeader();
        var exception = new Exception("Test exception");
        var message = Mock.Of<IRequest>();

        // Act
        await _consumer.FallbackAsync(messageHeader, message, exception);

        // Assert
        _eventMiddlewareMock.Verify(m => m.FallbackAsync(messageHeader, message, exception), Times.Once);
    }
}
