using Maomi.MQ;
using ProtoDemo;

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
        await Task.Delay(5000);
        var messagePublisher = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IMessagePublisher>();

        while (true)
        {
            for (int i = 0; i < 1000; i++)
            {
                var index = Interlocked.Increment(ref _count);
                await messagePublisher.PublishAsync("o2", "proto_console1", message: new TestEvent
                {
                    Id = index,
                    Message = _message,
                    Data = _data
                });

                await messagePublisher.PublishAsync("o2", "proto_console2", message: new ProtoDemo.Proto.Person
                {
                    Id = index,
                    Name = "test",
                    Email = "test@test.com",
                });

            }

            Console.WriteLine($"Sent {_count}");
            await Task.Delay(1000);
        }
    }
}