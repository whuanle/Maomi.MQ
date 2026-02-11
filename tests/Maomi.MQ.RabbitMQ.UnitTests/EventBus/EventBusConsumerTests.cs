using Maomi.MQ;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maomi.MQ.RabbitMQ.UnitTests.EventBus;

public class EventBusConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldDelegateToMiddleware()
    {
        var middleware = new Mock<IEventMiddleware<TestMessage>>(MockBehavior.Strict);
        var mediator = new Mock<IHandlerMediator<TestMessage>>(MockBehavior.Strict);

        middleware
            .Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestMessage>(), It.IsAny<EventHandlerDelegate<TestMessage>>()))
            .Returns(Task.CompletedTask);

        var consumer = new EventBusConsumer<TestMessage>(middleware.Object, mediator.Object, NullLoggerFactory.Instance);

        await consumer.ExecuteAsync(new MessageHeader(), new TestMessage());

        middleware.Verify(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestMessage>(), It.IsAny<EventHandlerDelegate<TestMessage>>()), Times.Once);
    }

    [Fact]
    public async Task FaildAsync_ShouldDelegateToMiddleware()
    {
        var middleware = new Mock<IEventMiddleware<TestMessage>>(MockBehavior.Strict);
        var mediator = new Mock<IHandlerMediator<TestMessage>>(MockBehavior.Strict);

        middleware
            .Setup(x => x.FaildAsync(It.IsAny<MessageHeader>(), It.IsAny<Exception>(), It.IsAny<int>(), It.IsAny<TestMessage?>()))
            .Returns(Task.CompletedTask);

        var consumer = new EventBusConsumer<TestMessage>(middleware.Object, mediator.Object, NullLoggerFactory.Instance);

        await consumer.FaildAsync(new MessageHeader(), new Exception("boom"), 2, new TestMessage());

        middleware.Verify(x => x.FaildAsync(It.IsAny<MessageHeader>(), It.IsAny<Exception>(), It.IsAny<int>(), It.IsAny<TestMessage?>()), Times.Once);
    }

    [Fact]
    public async Task FallbackAsync_ShouldDelegateToMiddleware()
    {
        var middleware = new Mock<IEventMiddleware<TestMessage>>(MockBehavior.Strict);
        var mediator = new Mock<IHandlerMediator<TestMessage>>(MockBehavior.Strict);

        middleware
            .Setup(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestMessage?>(), It.IsAny<Exception?>()))
            .ReturnsAsync(ConsumerState.Nack);

        var consumer = new EventBusConsumer<TestMessage>(middleware.Object, mediator.Object, NullLoggerFactory.Instance);

        var result = await consumer.FallbackAsync(new MessageHeader(), new TestMessage(), new Exception("boom"));

        Assert.Equal(ConsumerState.Nack, result);
        middleware.Verify(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestMessage?>(), It.IsAny<Exception?>()), Times.Once);
    }

    public sealed class TestMessage
    {
    }
}
