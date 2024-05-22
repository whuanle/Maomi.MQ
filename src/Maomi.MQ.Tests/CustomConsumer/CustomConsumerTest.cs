using Maomi.MQ.Defaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.Tests.CustomConsumer;

public class CustomConsumerTest
{
    [Fact]
    public async Task Test1()
    {
        using var host = BuildHost();
        var services = host.Services;
        _ = host.RunAsync();

        var waitReadyFactory = services.GetRequiredService<IWaitReadyFactory>();

        await waitReadyFactory.WaitReady();


    }

    public IHost BuildHost()
    {
        var host = new HostBuilder()
            .ConfigureLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            })
            .ConfigureHostConfiguration(options =>
            {
                options
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json");
            })
            .ConfigureServices((context, services) =>
            {
                var rabbitmqHostName = context.Configuration["RabbitMQ:HostName"]!;

                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                }, options =>
                {
                    options.HostName = rabbitmqHostName;
                },
                [typeof(CustomConsumerTest).Assembly],
                Array.Empty<EmptyTypeFilter>());
            }).Build();

        return host;
    }
}

public class CustomConsumerService : IConsumer<CustomConsumerTestEvent>
{
    public async Task ExecuteAsync(EventBody<CustomConsumerTestEvent> message)
    {

    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<CustomConsumerTestEvent>? message)
    {
        return Task.CompletedTask;
    }

    public Task<bool> FallbackAsync(EventBody<CustomConsumerTestEvent>? message)
    {
        return Task.FromResult(true) ;
    }
}

public class CustomConsumerTestEvent
{
    public string Message { get; set; }
}

/*
 
*/