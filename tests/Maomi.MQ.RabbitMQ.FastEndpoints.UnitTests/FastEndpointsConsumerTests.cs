using FastEndpoints;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maomi.MQ.RabbitMQ.FastEndpoints.UnitTests;

public class FastEndpointsConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldInvokeMiddleware_WhenMessageIsEvent()
    {
        // Arrange
        MessageHeader messageHeader = new();
        TestEvent message = new();
        Mock<IEventMiddleware<TestEvent>> eventMiddlewareMock = new();
        Mock<ILogger<FastEndpointsConsumer<TestEvent>>> loggerMock = new();
        EventHandlerDelegate<TestEvent>? capturedDelegate = null;

        eventMiddlewareMock
            .Setup(x => x.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<TestEvent>>()))
            .Callback<MessageHeader, TestEvent, EventHandlerDelegate<TestEvent>>((_, _, next) => capturedDelegate = next)
            .Returns(Task.CompletedTask);

        FastEndpointsConsumer<TestEvent> consumer = new(eventMiddlewareMock.Object, loggerMock.Object);

        // Act
        await consumer.ExecuteAsync(messageHeader, message);
        await Task.CompletedTask;

        // Assert
        Assert.NotNull(capturedDelegate);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInvokeMiddleware_WhenMessageIsCommand()
    {
        // Arrange
        MessageHeader messageHeader = new();
        TestCommand message = new();
        Mock<IEventMiddleware<TestCommand>> eventMiddlewareMock = new();
        Mock<ILogger<FastEndpointsConsumer<TestCommand>>> loggerMock = new();
        EventHandlerDelegate<TestCommand>? capturedDelegate = null;

        eventMiddlewareMock
            .Setup(x => x.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<TestCommand>>()))
            .Callback<MessageHeader, TestCommand, EventHandlerDelegate<TestCommand>>((_, _, next) => capturedDelegate = next)
            .Returns(Task.CompletedTask);

        FastEndpointsConsumer<TestCommand> consumer = new(eventMiddlewareMock.Object, loggerMock.Object);

        // Act
        await consumer.ExecuteAsync(messageHeader, message);
        await Task.CompletedTask;

        // Assert
        Assert.NotNull(capturedDelegate);
    }

    [Fact]
    public async Task FaildAsync_ShouldCallMiddlewareFaildAsync()
    {
        // Arrange
        MessageHeader messageHeader = new();
        Exception exception = new("test");
        TestCommand message = new();
        Mock<IEventMiddleware<TestCommand>> eventMiddlewareMock = new();
        Mock<ILogger<FastEndpointsConsumer<TestCommand>>> loggerMock = new();

        eventMiddlewareMock
            .Setup(x => x.FaildAsync(messageHeader, exception, 3, message))
            .Returns(Task.CompletedTask);

        FastEndpointsConsumer<TestCommand> consumer = new(eventMiddlewareMock.Object, loggerMock.Object);

        // Act
        await consumer.FaildAsync(messageHeader, exception, 3, message);

        // Assert
        eventMiddlewareMock.Verify(x => x.FaildAsync(messageHeader, exception, 3, message), Times.Once);
    }

    [Fact]
    public async Task FallbackAsync_ShouldCallMiddlewareFallbackAsync()
    {
        // Arrange
        MessageHeader messageHeader = new();
        Exception exception = new("test");
        TestCommand message = new();
        Mock<IEventMiddleware<TestCommand>> eventMiddlewareMock = new();
        Mock<ILogger<FastEndpointsConsumer<TestCommand>>> loggerMock = new();

        eventMiddlewareMock
            .Setup(x => x.FallbackAsync(messageHeader, message, exception))
            .ReturnsAsync(ConsumerState.Ack);

        FastEndpointsConsumer<TestCommand> consumer = new(eventMiddlewareMock.Object, loggerMock.Object);

        // Act
        ConsumerState state = await consumer.FallbackAsync(messageHeader, message, exception);

        // Assert
        Assert.Equal(ConsumerState.Ack, state);
        eventMiddlewareMock.Verify(x => x.FallbackAsync(messageHeader, message, exception), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogWarning_WhenMessageTypeNotSupported()
    {
        // Arrange
        MessageHeader messageHeader = new();
        UnsupportedMessage message = new();
        Mock<IEventMiddleware<UnsupportedMessage>> eventMiddlewareMock = new();
        Mock<ILogger<FastEndpointsConsumer<UnsupportedMessage>>> loggerMock = new();
        EventHandlerDelegate<UnsupportedMessage>? capturedDelegate = null;

        eventMiddlewareMock
            .Setup(x => x.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<UnsupportedMessage>>()))
            .Callback<MessageHeader, UnsupportedMessage, EventHandlerDelegate<UnsupportedMessage>>((_, _, next) => capturedDelegate = next)
            .Returns(Task.CompletedTask);

        FastEndpointsConsumer<UnsupportedMessage> consumer = new(eventMiddlewareMock.Object, loggerMock.Object);

        // Act
        await consumer.ExecuteAsync(messageHeader, message);
        Assert.NotNull(capturedDelegate);
        await capturedDelegate!(messageHeader, message, CancellationToken.None);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("not supported")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public sealed class UnsupportedMessage
    {
    }

    public sealed class TestCommand : ICommand
    {
        public int ExecuteCount { get; private set; }

        public Task ExecuteAsync(CancellationToken ct)
        {
            ExecuteCount++;
            return Task.CompletedTask;
        }
    }

    public sealed class TestEvent : IEvent
    {
        public int PublishCount { get; private set; }

        public Task PublishAsync(Mode waitMode = Mode.WaitForNone, CancellationToken cancellationToken = default)
        {
            PublishCount++;
            return Task.CompletedTask;
        }
    }
}
