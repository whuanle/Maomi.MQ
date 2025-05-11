 
using Maomi.MQ;  
using Maomi.MQ.EventBus;
using Microsoft.Extensions.Logging;
using Moq;  
using Xunit;  

public class FastEndpointsConsumerTests  
{  
    private readonly Mock<IEventMiddleware<object>> _eventMiddlewareMock;  
    private readonly Mock<ILogger<FastEndpointsConsumer<object>>> _loggerMock;  
    private readonly FastEndpointsConsumer<object> _consumer;  

    public FastEndpointsConsumerTests()  
    {  
        _eventMiddlewareMock = new Mock<IEventMiddleware<object>>();  
        _loggerMock = new Mock<ILogger<FastEndpointsConsumer<object>>>();  
        _consumer = new FastEndpointsConsumer<object>(_eventMiddlewareMock.Object, _loggerMock.Object);  
    }  

    [Fact]  
    public async Task ExecuteAsync_Should_Invoke_EventMiddleware_ExecuteAsync()  
    {  
        // Arrange  
        var messageHeader = new MessageHeader();  
        var message = new object();  
        EventHandlerDelegate<object> capturedDelegate = null;  

        _eventMiddlewareMock  
            .Setup(m => m.ExecuteAsync(messageHeader, message, It.IsAny<EventHandlerDelegate<object>>()))  
            .Callback<MessageHeader, object, EventHandlerDelegate<object>>((_, _, next) => capturedDelegate = next)  
            .Returns(Task.CompletedTask);  

        // Act  
        await _consumer.ExecuteAsync(messageHeader, message);  

        // Assert  
        Assert.NotNull(capturedDelegate);  
        await capturedDelegate.Invoke(messageHeader, message, default);  
    }  

    [Fact]  
    public async Task FaildAsync_Should_Invoke_EventMiddleware_FallbackAsync()  
    {  
        // Arrange  
        var messageHeader = new MessageHeader();  
        var exception = new Exception("Test exception");  
        var retryCount = 3;  
        var message = new object();  

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
        var message = new object();  

        // Act  
        await _consumer.FallbackAsync(messageHeader, message, exception);  

        // Assert  
        _eventMiddlewareMock.Verify(m => m.FallbackAsync(messageHeader, message, exception), Times.Once);  
    }  
}  
