using Maomi.MQ;
using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.BatchPublisher.Worker;

[QueueName("example.batch.metrics")]
public sealed class MetricMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    public int Value { get; set; }
}

[Consumer("example.batch.metrics", Qos = 20)]
public sealed class MetricConsumer : IConsumer<MetricMessage>
{
    private readonly ILogger<MetricConsumer> _logger;

    public MetricConsumer(ILogger<MetricConsumer> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(MessageHeader messageHeader, MetricMessage message)
    {
        _logger.LogInformation(
            "Metric consumed. HeaderId={HeaderId}, MessageId={MessageId}, Value={Value}",
            messageHeader.Id,
            message.Id,
            message.Value);
        return Task.CompletedTask;
    }

    public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, MetricMessage message)
    {
        _logger.LogWarning(ex, "Metric consume failed. HeaderId={HeaderId}, RetryCount={RetryCount}", messageHeader.Id, retryCount);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, MetricMessage? message, Exception? ex)
    {
        _logger.LogError(ex, "Metric fallback. HeaderId={HeaderId}, MessageId={MessageId}", messageHeader.Id, message?.Id);
        return Task.FromResult(ConsumerState.Ack);
    }
}

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch publisher worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            var batch = Enumerable.Range(1, 10)
                .Select(index => new MetricMessage
                {
                    Value = Random.Shared.Next(1, 1000),
                    At = DateTimeOffset.UtcNow.AddMilliseconds(index)
                })
                .ToArray();

            foreach (var message in batch)
            {
                await publisher.AutoPublishAsync(message, cancellationToken: stoppingToken);
            }

            _logger.LogInformation("Published batch size={Size}", batch.Length);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
