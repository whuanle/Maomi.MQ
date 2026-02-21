using Maomi.MQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.Samples.ScenarioHub;

public sealed class BatchPublisherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BatchPublisherBackgroundService> _logger;
    private volatile bool _enabled;

    public BatchPublisherBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BatchPublisherBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool IsEnabled => _enabled;

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _logger.LogInformation("Batch publisher worker {State}.", enabled ? "enabled" : "disabled");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_enabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                var batch = Enumerable.Range(1, 10)
                    .Select(x => new MetricMessage
                    {
                        Value = Random.Shared.Next(1, 5000),
                        At = DateTimeOffset.UtcNow.AddMilliseconds(x)
                    })
                    .ToArray();

                foreach (var message in batch)
                {
                    await publisher.AutoPublishAsync(message, cancellationToken: stoppingToken);
                }

                _logger.LogInformation("Batch worker published {Count} messages.", batch.Length);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch worker publish loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
