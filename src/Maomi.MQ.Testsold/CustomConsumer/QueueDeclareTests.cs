using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Maomi.MQ.Tests.CustomConsumer;

public partial class QueueDeclareTests : BaseMock
{
    [Fact]
    public async Task QueueDeclare()
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
        services.RemoveAll<IHostedService>();

        var typeFilter = new ConsumerTypeFilter();
        typeFilter.Filter(services, typeof(AllOptionsConsumer));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var host = ioc.GetRequiredService<IHostedService>();
        _ = host.StartAsync(CancellationToken.None);

        var waitReady = ioc.GetRequiredService<IWaitReadyFactory>();
        await waitReady.WaitReadyAsync();

        // check arguments.
        _mockChannel.Verify(a => a.QueueDeclareAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(1000, arguments["x-expires"]);
        Assert.Equal(string.Empty, arguments["x-dead-letter-exchange"]);
        Assert.Equal("test_dead", arguments["x-dead-letter-routing-key"]);
    }



    [Fact]
    public async Task DynamicConsumerOptions()
    {
        ServiceCollection services = Mock();
        services.RemoveAll<IHostedService>();

        var typeFilter = new ConsumerTypeFilter((options, type) =>
        {
            if (type == typeof(AllOptionsConsumer))
            {
                options.Queue = options.Queue + "_1";
            }
            return true;
        });

        typeFilter.Filter(services, typeof(AllOptionsConsumer));
        typeFilter.Build(services);

        var ioc = services.BuildServiceProvider();
        var host = ioc.GetRequiredService<IHostedService>();
        _ = host.StartAsync(CancellationToken.None);
        var waitReady = ioc.GetRequiredService<IWaitReadyFactory>();
        await waitReady.WaitReadyAsync();

        var consumerOptions = ioc.GetRequiredKeyedService<IConsumerOptions>("test_1");
        Assert.NotNull(consumerOptions);
    }

}
