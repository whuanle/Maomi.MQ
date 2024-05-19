
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maomi.MQ.Tests.CustomConsumer;

public class CustomConsumerTest
{
    [Fact]
    public async Task Test1()
    {
        var testService = new TestsService();
        using var host = testService.BuildHost();
        var services = host.Services;
        _ = host.RunAsync();

        var waitReadyFactory = services.GetRequiredService<IWaitReadyFactory>();

        await waitReadyFactory.Wait();


    }
}

public class CustomConsumerService : IConsumer<CustomConsumerTestEvent>
{
    public async Task ExecuteAsync(EventBody<CustomConsumerTestEvent> message)
    {

    }

    public Task FaildAsync(EventBody<CustomConsumerTestEvent>? message)
    {
        return Task.CompletedTask;
    }

    public Task FallbackAsync(EventBody<CustomConsumerTestEvent>? message)
    {
        return Task.CompletedTask;
    }
}

public class CustomConsumerTestEvent
{
    public string Message { get; set; }
}