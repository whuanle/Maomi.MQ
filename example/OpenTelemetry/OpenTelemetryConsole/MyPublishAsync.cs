using Maomi.MQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

public class MyPublishAsync : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _message = string.Join(",", Enumerable.Range(0, 100));
    private readonly int[] _data = Enumerable.Range(0, 100).ToArray();
    private volatile int _count = 0;

    public MyPublishAsync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Start servics.");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messagePublisher = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IMessagePublisher>();

        while (true)
        {
            for (int i = 0; i < 1000; i++)
            {
                var index = Interlocked.Increment(ref _count);

                await messagePublisher.PublishAsync("o1", "opentelemetry_console", message: new TestEvent
                {
                    Id = index,
                    Message = _message,
                    Data = _data
                });
                await messagePublisher.PublishAsync("o1", "opentelemetry_console2", message: new TestEvent
                {
                    Id = index,
                    Message = _message,
                    Data = _data
                });
                await messagePublisher.PublishAsync("o1", "opentelemetry_console3", message: new TestEvent
                {
                    Id = index,
                    Message = _message,
                    Data = _data
                });

                //await messagePublisher.PublishAsync("o1", "opentelemetry_console4", message: new TestEvent
                //{
                //    Id = index,
                //    Message = _message,
                //    Data = _data
                //});
            }

            Console.WriteLine($"Sent {_count}");
            await Task.Delay(1000);
        }
    }
}