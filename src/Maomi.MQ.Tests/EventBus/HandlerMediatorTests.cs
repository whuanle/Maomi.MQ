using AutoFixture.Xunit2;
using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Maomi.MQ.Tests;

public class HandlerMediatorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IEventHandlerFactory<TestMessage>> _eventHandlerFactoryMock;
    private readonly ILoggerFactory _loggerFactory;

    public HandlerMediatorTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _eventHandlerFactoryMock = new Mock<IEventHandlerFactory<TestMessage>>();
        _loggerFactory = new NullLoggerFactory();
    }

    [Theory, AutoData]
    public async Task ExecuteAsync_ShouldExecuteHandlersInOrder(MessageHeader messageHeader)
    {
        ServiceCollection services = new();
        services.AddLogging();

        var message = new TestMessage();
        var cancellationToken = CancellationToken.None;

        var handler1 = new Mock<T1>();
        var handler2 = new Mock<T2>();

        var h1Type = handler1.Object.GetType();
        var h2Type = handler2.Object.GetType();
        var handlers = new Dictionary<int, Type>
        {
            { 1, h1Type },
            { 2, h2Type }
        };

        services.AddSingleton(h1Type, s => handler1.Object);
        services.AddSingleton(h2Type, s => handler2.Object);

        _eventHandlerFactoryMock.Setup(ehf => ehf.Handlers).Returns(handlers);

        var mediator = new HandlerMediator<TestMessage>(services.BuildServiceProvider(), _eventHandlerFactoryMock.Object, _loggerFactory);

        await mediator.ExecuteAsync(messageHeader, message, cancellationToken);

        handler1.Verify(h => h.ExecuteAsync(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        handler2.Verify(h => h.ExecuteAsync(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ExecuteAsync_ShouldRollbackOnException(MessageHeader messageHeader)
    {
        var message = new TestMessage();
        var cancellationToken = CancellationToken.None;

        var handler1 = new Mock<IEventHandler<TestMessage>>();
        var handler2 = new Mock<IEventHandler<TestMessage>>();
        handler2.Setup(h => h.ExecuteAsync(message, cancellationToken)).ThrowsAsync(new Exception("Test exception"));

        var handlers = new Dictionary<int, Type>
        {
            { 1, handler1.Object.GetType() },
            { 2, handler2.Object.GetType() }
        };

        _eventHandlerFactoryMock.Setup(ehf => ehf.Handlers).Returns(handlers);
        _serviceProviderMock.Setup(sp => sp.GetService(handler1.Object.GetType())).Returns(handler1.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(handler2.Object.GetType())).Returns(handler2.Object);

        var mediator = new HandlerMediator<TestMessage>(_serviceProviderMock.Object, _eventHandlerFactoryMock.Object, _loggerFactory);

        await Assert.ThrowsAsync<Exception>(() => mediator.ExecuteAsync(messageHeader, message, cancellationToken));

        handler1.Verify(h => h.ExecuteAsync(message, cancellationToken), Times.Once);
        handler2.Verify(h => h.ExecuteAsync(message, cancellationToken), Times.Once);
        handler2.Verify(h => h.CancelAsync(message, cancellationToken), Times.Once);
        handler1.Verify(h => h.CancelAsync(message, cancellationToken), Times.Once);
    }

    [EventTopic("test")]
    public class TestMessage { }

    public interface T1: IEventHandler<TestMessage> { }
    public interface T2 : IEventHandler<TestMessage> { }
}
