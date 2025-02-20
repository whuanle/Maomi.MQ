using Maomi.MQ;
using QosPublisher.Controllers;
using System.Reflection;

namespace QosConsole;

public class Program
{
    public static async Task Main()
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
                        options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
                        options.Port = 5672;
                        options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
                        options.ConsumerDispatchConcurrency = 1000;
                    };
                }, new System.Reflection.Assembly[] { typeof(Program).Assembly });

            }).Build();

        Console.WriteLine($"start time:{DateTime.Now}");
        await host.RunAsync();
    }
}


[Consumer("qos", Qos = 1000)]
public class QosConsumer : IConsumer<TestEvent>
{
    private static int Count = 0;

    public async Task ExecuteAsync(MessageHeader messageHeader, TestEvent message)
    {
        Interlocked.Increment(ref Count);
        Console.WriteLine($"date time:{DateTime.Now},id:{message.Id}, count:{Count}");
        await Task.Delay(50);
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, TestEvent message)
    {
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, TestEvent? message, Exception? ex)
    {
        return Task.FromResult(ConsumerState.Ack);
    }
}