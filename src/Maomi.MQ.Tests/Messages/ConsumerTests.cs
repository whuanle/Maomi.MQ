using AutoFixture;
using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Maomi.MQ.Pool;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;

namespace Maomi.MQ.Tests.Messages;

/*
 Simulated consumption scenario.
 */

public partial class ConsumerTests
{
    [Fact]
    public async Task TestMessageConsumer_WhenReturnACK()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true};

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        Assert.Equal(testEvent.ValueA, consumerMock.Object.Message.ValueA);
        Assert.Equal(testEvent.ValueB, consumerMock.Object.Message.ValueB);
        Assert.Equal(testEvent.ValueC, consumerMock.Object.Message.ValueC);
        Assert.Equal(testEvent.ValueD, consumerMock.Object.Message.ValueD);
        Assert.Equal(testEvent.ValueE, consumerMock.Object.Message.ValueE);
        Assert.Equal(testEvent.ValueF, consumerMock.Object.Message.ValueF);
        Assert.Equal(testEvent.ValueG, consumerMock.Object.Message.ValueG);
        Assert.Equal(testEvent.ValueH, consumerMock.Object.Message.ValueH);
        Assert.Equal(testEvent.ValueI, consumerMock.Object.Message.ValueI);
        Assert.Equal(testEvent.ValueJ, consumerMock.Object.Message.ValueJ);
        Assert.Equal(testEvent.ValueK, consumerMock.Object.Message.ValueK);
        Assert.Equal(testEvent.ValueL, consumerMock.Object.Message.ValueL);
        Assert.Equal(testEvent.ValueM, consumerMock.Object.Message.ValueM);

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicAckAsync(1111, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        consumerMock.Verify(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>()), Times.Once);
        consumerMock.Verify(x => x.FaildAsync(It.IsAny<MessageHeader>(), It.IsAny<Exception>(), It.IsAny<int>(), It.IsAny<TestEvent>()), Times.Never);
        consumerMock.Verify(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>()), Times.Never);
    }

    [Fact]
    public async Task TestMessageConsumer_WhenExecuteExecption()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true };

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        consumerMock.Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>())).Throws(new Exception("Test"));

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicAckAsync(1111, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
        consumerMock.Verify(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>()), Times.Exactly(4));
        consumerMock.Verify(x => x.FaildAsync(It.IsAny<MessageHeader>(), It.IsAny<Exception>(), It.IsAny<int>(), It.IsAny<TestEvent>()), Times.Exactly(4));
        consumerMock.Verify(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task TestMessageConsumer_CorrectlyIdentifyFallbackState_ReturnNack()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true };

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        consumerMock.Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>())).Throws(new Exception("Test"));
        consumerMock.Setup(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>())).Returns(Task.FromResult(ConsumerState.Nack));

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicNackAsync(1111, false, consumerOptions.RetryFaildRequeue, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestMessageConsumer_CorrectlyIdentifyFallbackState_ReturnNackAndRequeue()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true };

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        consumerMock.Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>())).Throws(new Exception("Test"));
        consumerMock.Setup(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>())).Returns(Task.FromResult(ConsumerState.NackAndRequeue));

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicNackAsync(1111, false, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestMessageConsumer_CorrectlyIdentifyFallbackState_ReturnNackAndNoRequeue()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true };

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        consumerMock.Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>())).Throws(new Exception("Test"));
        consumerMock.Setup(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>())).Returns(Task.FromResult(ConsumerState.NackAndNoRequeue));

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicNackAsync(1111, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TestMessageConsumer_CorrectlyIdentifyFallbackState_ReturnException()
    {
        RabbitMQConnectionMock rabbitMQConnectionMock = new RabbitMQConnectionMock();
        ServiceCollection services = BuildServiceCollection(rabbitMQConnectionMock);

        var serviceProvider = services.BuildServiceProvider();

        var fixture = new AutoFixture.Fixture();
        var consumerOptions = fixture.Create<ConsumerOptions>();
        var testEvent = fixture.Create<TestEvent>();
        var consumerMock = new Mock<TestConsumer<TestEvent>>() { CallBase = true };

        MessageConsumer messageConsumer = new MessageConsumer(serviceProvider, consumerOptions, s =>
        {
            return consumerMock.Object;
        });

        var jsonSerializer = serviceProvider.GetRequiredService<IMessageSerializer>();
        var bytes = jsonSerializer.Serializer(testEvent);
        var buffer = new byte[2000];
        IAmqpWriteable basicProperties = new BasicProperties { Persistent = true, AppId = "AppId", ContentEncoding = jsonSerializer.ContentEncoding, ContentType = jsonSerializer.ContentType };
        int offset = basicProperties.WriteTo(buffer);

        consumerMock.Setup(x => x.ExecuteAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>())).Throws(new Exception("Test"));
        consumerMock.Setup(x => x.FallbackAsync(It.IsAny<MessageHeader>(), It.IsAny<TestEvent>(), It.IsAny<Exception>())).Throws(new Exception("Test"));

        await messageConsumer.ConsumerAsync<TestEvent>(
            rabbitMQConnectionMock.ChannelMock.Object,
            new BasicDeliverEventArgs("000", deliveryTag: 1111, false, consumerOptions.BindExchange!, consumerOptions.RoutingKey!, new ReadOnlyBasicProperties(buffer.AsSpan()), bytes));

        rabbitMQConnectionMock.ChannelMock.Verify(x => x.BasicNackAsync(1111, false, consumerOptions.RetryFaildRequeue, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ServiceCollection BuildServiceCollection(RabbitMQConnectionMock rabbitMQConnectionMock)
    {
        var services = new ServiceCollection();
        services.AddMaomiMQCore();
        services.AddSingleton<IRetryPolicyFactory, MockDefaultRetryPolicyFactory>();
        services.AddLogging();
        services.AddSingleton(rabbitMQConnectionMock.MqOptions);
        services.AddScoped<ServiceFactory>();
        services.AddScoped<IBreakdown, DefaultBreakdown>();
        services.AddSingleton<ConnectionPool>(rabbitMQConnectionMock.ConnectionPoolMock.Object);
        services.AddSingleton<IRoutingProvider, RoutingProvider>();
        return services;
    }
}

public partial class ConsumerTests
{
    public class TestEvent
    {
        public bool ValueA { get; set; }
        public sbyte ValueB { get; set; }
        public byte ValueC { get; set; }
        public short ValueD { get; set; }
        public ushort ValueE { get; set; }
        public int ValueF { get; set; }
        public uint ValueG { get; set; }
        public long ValueH { get; set; }
        public ulong ValueI { get; set; }
        public float ValueJ { get; set; }
        public double ValueK { get; set; }
        public decimal ValueL { get; set; }
        public char ValueM { get; set; }
    }

    public class TestConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
    {
        public TMessage Message { get; private set; } = default!;

        public int RetryCount { get; private set; }
        public bool IsFallbacked { get; private set; }

        public virtual Task ExecuteAsync(MessageHeader messageHeader, TMessage message)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public virtual Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TMessage message)
        {
            RetryCount++;
            return Task.CompletedTask;
        }

        public virtual Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TMessage? message, Exception? ex)
        {
            IsFallbacked = true;
            return Task.FromResult(ConsumerState.Ack);
        }
    }

    private class MockDefaultRetryPolicyFactory : DefaultRetryPolicyFactory
    {
        public MockDefaultRetryPolicyFactory(ILogger<DefaultRetryPolicyFactory> logger) : base(logger)
        {
            var fieldInfo = typeof(DefaultRetryPolicyFactory).GetField("RetryBaseDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo!.SetValue(this, 1);
        }
    }
}