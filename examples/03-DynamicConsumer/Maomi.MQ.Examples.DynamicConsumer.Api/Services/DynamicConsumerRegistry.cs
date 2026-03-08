using System.Collections.Concurrent;

namespace Maomi.MQ.Examples.DynamicConsumer.Api.Services;

public sealed class DynamicConsumerRegistry
{
    public ConcurrentDictionary<string, string> ConsumerTags { get; } = new(StringComparer.OrdinalIgnoreCase);
}
