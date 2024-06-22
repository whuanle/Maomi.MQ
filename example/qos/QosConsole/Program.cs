using Maomi.MQ;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using QosPublisher.Controllers;

class Program
{
    static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureLogging(options =>
            {
                options.AddConsole();
                options.AddDebug();
            })
            .ConfigureServices(services =>
            {
                services.AddMaomiMQ(options =>
                {
                    options.WorkId = 1;
                    options.AppName = "myapp-consumer";
                    options.Rabbit = (options) =>
                    {
                        options.HostName = "10.1.0.4";
                        options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
                    };
                }, new System.Reflection.Assembly[] { typeof(Program).Assembly });

            }).Build();

        Console.WriteLine($"start time:{DateTime.Now}");
        await host.RunAsync();
    }
}


[Consumer("qos", Qos = 30)]
public class QosConsumer : IConsumer<TestEvent>
{
    private static int Count = 0;
    public async Task ExecuteAsync(EventBody<TestEvent> message)
    {
        Interlocked.Increment(ref Count);
        Console.WriteLine($"date time:{DateTime.Now},id:{message.Body.Id}, count:{Count}");
        await Task.Delay(50);
    }

    public Task FaildAsync(Exception ex, int retryCount, EventBody<TestEvent>? message)
    {
        return Task.CompletedTask;
    }

    public Task<bool> FallbackAsync(EventBody<TestEvent>? message)
    {
        return Task.FromResult(true);
    }
}