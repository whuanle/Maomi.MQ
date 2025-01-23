using Maomi.MQ.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Net.Sockets;
using System.Reflection;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class ConsumerReadyTest : BaseMock
{
    [Fact]
    public async Task WaitReady()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter1 = new ConsumerTypeFilter();
                typeFilter1.Filter(services, typeof(MyConsumer));
                typeFilter1.Build(services);

                services.AddSingleton(_mockConnectionFactory.Object);

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                    options.Rabbit = rabbit => { };
                }, Array.Empty<Assembly>(), [typeFilter1]);

                services.AddSingleton<MqOptions>(new MqOptions
                {
                    AppName = "test",
                    WorkId = 0,
                    ConnectionFactory = _mockConnectionFactory.Object
                });
            }).Build();

        var waitReadyFactory = host.Services.GetRequiredService<IWaitReadyFactory>();

        _ = host.RunAsync();
        var task = waitReadyFactory.WaitReadyAsync();
        await task;
        Assert.True(task.IsCompleted);

        _mockChannel.Verify(a => a.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WaitReady_Some()
    {
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter1 = new ConsumerTypeFilter();
                typeFilter1.Filter(services, typeof(MyConsumer));
                typeFilter1.Build(services);

                var typeFilter2 = new EventBusTypeFilter();
                typeFilter2.Filter(services, typeof(TestEventHandler));
                typeFilter2.Build(services);

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
            }).Build();

        var waitReadyFactory = host.Services.GetRequiredService<IWaitReadyFactory>();

        _ = host.RunAsync();
        var task = waitReadyFactory.WaitReadyAsync();
        await task;
        Assert.True(task.IsCompleted);

        _mockChannel.Verify(a => a.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WaitReady_Exception()
    {
        _mockChannel
            .Setup(c => c.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Throws(new SocketException(400, "error"));

        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                var typeFilter = new ConsumerTypeFilter();
                typeFilter.Filter(services, typeof(MyConsumer));
                typeFilter.Build(services);

                services.AddSingleton(_mockConnectionFactory.Object);

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                    options.Rabbit = rabbit => { };
                }, Array.Empty<Assembly>(), [typeFilter]);

                services.AddSingleton<MqOptions>(new MqOptions
                {
                    AppName = "test",
                    WorkId = 0,
                    ConnectionFactory = _mockConnectionFactory.Object
                });
            }).Build();

        var waitReadyFactory = host.Services.GetRequiredService<IWaitReadyFactory>();

        _ = host.RunAsync();
        var task = waitReadyFactory.WaitReadyAsync();
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Assert.True(ex is SocketException);
        }
        Assert.IsType<SocketException>(task.Exception?.InnerExceptions?.FirstOrDefault());
        await Assert.ThrowsAsync<SocketException>(async () => await task);
    }
}
