using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using static Maomi.MQ.Diagnostics.DiagnosticName;

namespace Maomi.MQ.Tests.CustomConsumer;

public class BaseHostTest
{
    public readonly Mock<IConnectionFactory> _mockConnectionFactory = new();
    public readonly Mock<IConnection> _mockConnection = new Mock<IConnection>();
    public readonly Mock<IChannel> _mockChannel = new Mock<IChannel>();

    public BaseHostTest()
    {
        _mockConnectionFactory
            .Setup(c => c.CreateConnectionAsync(CancellationToken.None))
            .Returns(Task.FromResult(_mockConnection.Object));
        _mockConnection
            .Setup(c => c.CreateChannelAsync(CancellationToken.None))
            .Returns(Task.FromResult(_mockChannel.Object));

        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicQosAsync(It.IsAny<uint>(), It.IsAny<ushort>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
        _mockChannel
            .Setup(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()));
    }

    public ServiceCollection Mock()
    {
        ServiceCollection services = new();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // mock.
        services.AddSingleton(_mockConnectionFactory.Object);
        services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
            options.Rabbit = rabbit => { };
        }, Array.Empty<Assembly>());
        services.AddSingleton<MqOptions>(new MqOptions
        {
            AppName = "test",
            WorkId = 0,
            ConnectionFactory = _mockConnectionFactory.Object
        });
        return services;
    }

    public class ExceptionJsonSerializer : IJsonSerializer
    {
        public TObject? Deserialize<TObject>(ReadOnlySpan<byte> bytes) where TObject : class
        {
            throw new NotImplementedException();
        }

        public byte[] Serializer<TObject>(TObject obj) where TObject : class
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj);
        }
    }

    [Consumer("test")]
    public class UnSetConsumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
    where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; }

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public Task ExecuteAsync(EventBody<TEvent> message)
        {
            EventBody = message;
            return Task.CompletedTask;
        }
        public Task FaildAsync(Exception ex, int retryCount, EventBody<TEvent>? message)
        {
            RetryCount = retryCount;
            return Task.CompletedTask;
        }
        public Task<bool> FallbackAsync(EventBody<TEvent>? message)
        {
            IsFallbacked = true;
            return Task.FromResult(false);
        }
    }
}
