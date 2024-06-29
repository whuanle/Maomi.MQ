using Maomi.MQ;

public class MyPublishAsync : BackgroundService
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly string _message = string.Join(",", Enumerable.Range(0, 100));
    private readonly int[] _data = Enumerable.Range(0, 100).ToArray();
    private volatile int _count = 0;

    public MyPublishAsync(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Start servics.");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var func = async (int index) =>
        {
            await _messagePublisher.PublishAsync(queue: "opentelemetry_console", message: new TestEvent
            {
                Id = index,
                Message = _message,
                Data = _data
            });
            await _messagePublisher.PublishAsync(queue: "opentelemetry_console2", message: new TestEvent
            {
                Id = index,
                Message = _message,
                Data = _data
            });
            await _messagePublisher.PublishAsync(queue: "opentelemetry_console3", message: new TestEvent
            {
                Id = index,
                Message = _message,
                Data = _data
            });
        };

        while (true)
        {
            for(var i = 0; i < 100; i++)
            {
                var count = Interlocked.Increment(ref _count);

                _ = func.Invoke(count);
            }

            await Task.Delay(500);
        }
    }
}