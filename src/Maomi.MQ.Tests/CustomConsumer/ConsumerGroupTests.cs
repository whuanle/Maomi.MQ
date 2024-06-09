using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RabbitMQ.Client;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class ConsumerGroupTests : BaseHostTest
{
    [Fact]
    public async Task Group()
    {
        // get queue declare arguments.
        IDictionary<string, object> arguments = null!;
        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, bool, bool, IDictionary<string, object>, bool, bool, CancellationToken>((a1, a2, a3, a4, a5, a6, a7, a8) =>
            {
                arguments = a5;
            });

        ServiceCollection services = Mock();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(Consumer1));
        typeFilter.Filter(services, typeof(Consumer2));
        typeFilter.Build(services);
        var ioc = services.BuildServiceProvider();

        using var host = (ioc.GetRequiredService<IHostedService>() as ConsumerBaseHostService)!;
        _ = host.StartAsync(CancellationToken.None);
        await Task.Delay(1000);
        var waitReadyFactory =  ioc.GetRequiredService<IWaitReadyFactory>();
        await waitReadyFactory.WaitReadyAsync();

        _mockChannel.Verify(a => a.BasicConsumeAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<IBasicConsumer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

public partial class ConsumerGroupTests
{
    private class IdEvent
    {
        public int Id { get; set; }
    }

    private class BaseConsumer<TEvent> : IConsumer<TEvent>, IRetry, IEventBody<TEvent>
        where TEvent : class
    {
        public EventBody<TEvent> EventBody { get; private set; } = null!;

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

    [Consumer("test1", Group = "group")]
    private class Consumer1 : BaseConsumer<IdEvent> { }

    [Consumer("test2", Group = "group")]
    private class Consumer2 : BaseConsumer<IdEvent> { }
}
