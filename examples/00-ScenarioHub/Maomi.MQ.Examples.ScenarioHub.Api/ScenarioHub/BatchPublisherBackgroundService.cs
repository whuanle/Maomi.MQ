using Maomi.MQ;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace Maomi.MQ.Samples.ScenarioHub;

public sealed class BatchPublisherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScenarioRuntimeState _state;

    public BatchPublisherBackgroundService(IServiceProvider serviceProvider, ScenarioRuntimeState state)
    {
        _serviceProvider = serviceProvider;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_state.BatchPublisherEnabled)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

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

            Interlocked.Add(ref _state.BatchPublished, batch.Length);
            _state.AddLog($"batch published count={batch.Length}");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
