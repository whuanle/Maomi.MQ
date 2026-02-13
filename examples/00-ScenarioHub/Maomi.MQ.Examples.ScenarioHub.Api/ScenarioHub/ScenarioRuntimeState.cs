using System.Collections.Concurrent;

namespace Maomi.MQ.Samples.ScenarioHub;

public sealed class ScenarioRuntimeState
{
    public long QuickStartConsumed;

    public long EventBusConsumed;

    public long DynamicConsumed;

    public long RetryConsumed;

    public long RetryFailed;

    public long DeadLetterConsumed;

    public long ProtobufConsumed;

    public long BatchPublished;

    public long BatchConsumed;

    public long DynamicStarted;

    public long DynamicStopped;

    public ConcurrentQueue<string> Logs { get; } = new();

    public ConcurrentDictionary<string, string> DynamicConsumerTags { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool BatchPublisherEnabled { get; set; }

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public void AddLog(string message)
    {
        Logs.Enqueue($"{DateTimeOffset.UtcNow:O} {message}");
        while (Logs.Count > 200)
        {
            Logs.TryDequeue(out _);
        }
    }

    public ScenarioStatus Snapshot()
    {
        return new ScenarioStatus
        {
            StartedAt = StartedAt,
            BatchPublisherEnabled = BatchPublisherEnabled,
            Counters = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["quickstart_consumed"] = QuickStartConsumed,
                ["eventbus_consumed"] = EventBusConsumed,
                ["dynamic_consumed"] = DynamicConsumed,
                ["retry_consumed"] = RetryConsumed,
                ["retry_failed"] = RetryFailed,
                ["dead_letter_consumed"] = DeadLetterConsumed,
                ["protobuf_consumed"] = ProtobufConsumed,
                ["batch_published"] = BatchPublished,
                ["batch_consumed"] = BatchConsumed,
                ["dynamic_started"] = DynamicStarted,
                ["dynamic_stopped"] = DynamicStopped
            },
            DynamicConsumers = DynamicConsumerTags
                .Select(x => new DynamicConsumerInfo { Queue = x.Key, ConsumerTag = x.Value })
                .OrderBy(x => x.Queue)
                .ToArray(),
            RecentLogs = Logs.Reverse().Take(50).ToArray()
        };
    }
}

public sealed class ScenarioStatus
{
    public DateTimeOffset StartedAt { get; set; }

    public bool BatchPublisherEnabled { get; set; }

    public Dictionary<string, long> Counters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public DynamicConsumerInfo[] DynamicConsumers { get; set; } = Array.Empty<DynamicConsumerInfo>();

    public string[] RecentLogs { get; set; } = Array.Empty<string>();
}

public sealed class DynamicConsumerInfo
{
    public string Queue { get; set; } = string.Empty;

    public string ConsumerTag { get; set; } = string.Empty;
}
