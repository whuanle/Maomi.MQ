using Maomi.MQ.EventBus;
using Maomi.MQ.MediatR;
using MediatR;
using Moq;

namespace Maomi.MQ.RabbitMQ.MediatR.UnitTests;

public class MediatRConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeMediatorSend()
    {
        // Arrange
        MessageHeader messageHeader = new();
        TestRequest message = new();
        Mock<IEventMiddleware<TestRequest>> eventMiddlewareMock = new();
        Mock<IMediator> mediatorMock = new();
        EventHandlerDelegate<TestRequest>? capturedDelegate = null;

        eventMiddlewareMock
            .Setup(x => x.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<TestRequest>>()))
            .Callback<MessageHeader, TestRequest, EventHandlerDelegate<TestRequest>>((_, _, next) => capturedDelegate = next)
            .Returns(Task.CompletedTask);

        mediatorMock
            .Setup(x => x.Send(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        MediatRConsumer<TestRequest> consumer = new(eventMiddlewareMock.Object, mediatorMock.Object);

        // Act
        await consumer.ExecuteAsync(messageHeader, message);
        Assert.NotNull(capturedDelegate);
        await capturedDelegate!(messageHeader, message, CancellationToken.None);

        // Assert
        mediatorMock.Verify(x => x.Send(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FaildAsync_ShouldCallMiddlewareFaildAsync()
    {
        // Arrange
        MessageHeader messageHeader = new();
        Exception exception = new("test");
        TestRequest message = new();
        Mock<IEventMiddleware<TestRequest>> eventMiddlewareMock = new();
        Mock<IMediator> mediatorMock = new();

        eventMiddlewareMock
            .Setup(x => x.FaildAsync(messageHeader, exception, 2, message))
            .Returns(Task.CompletedTask);

        MediatRConsumer<TestRequest> consumer = new(eventMiddlewareMock.Object, mediatorMock.Object);

        // Act
        await consumer.FaildAsync(messageHeader, exception, 2, message);

        // Assert
        eventMiddlewareMock.Verify(x => x.FaildAsync(messageHeader, exception, 2, message), Times.Once);
    }

    [Fact]
    public async Task FallbackAsync_ShouldCallMiddlewareFallbackAsync()
    {
        // Arrange
        MessageHeader messageHeader = new();
        Exception exception = new("test");
        TestRequest message = new();
        Mock<IEventMiddleware<TestRequest>> eventMiddlewareMock = new();
        Mock<IMediator> mediatorMock = new();

        eventMiddlewareMock
            .Setup(x => x.FallbackAsync(messageHeader, message, exception))
            .ReturnsAsync(ConsumerState.Ack);

        MediatRConsumer<TestRequest> consumer = new(eventMiddlewareMock.Object, mediatorMock.Object);

        // Act
        ConsumerState state = await consumer.FallbackAsync(messageHeader, message, exception);

        // Assert
        Assert.Equal(ConsumerState.Ack, state);
        eventMiddlewareMock.Verify(x => x.FallbackAsync(messageHeader, message, exception), Times.Once);
    }

    public sealed class TestRequest : IRequest
    {
    }
}
