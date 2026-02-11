using Maomi.MQ;
using Maomi.MQ.Attributes;
using ProtoBuf;

namespace Maomi.MQ.Samples.ScenarioHub;

[QueueName("scenario.quickstart")]
public sealed class QuickStartMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}

[QueueName("scenario.eventbus.order")]
public sealed class OrderCreatedEvent
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public decimal Amount { get; set; }

    public string Customer { get; set; } = string.Empty;
}

[QueueName("scenario.dynamic.default")]
public sealed class DynamicMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;
}

[QueueName("scenario.retry.main")]
public sealed class RetryMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public bool ForceFail { get; set; } = true;
}

[QueueName("scenario.retry.dead")]
public sealed class RetryDeadMessage
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset DeadAt { get; set; } = DateTimeOffset.UtcNow;
}

[ProtoContract]
[QueueName("scenario.protobuf.person")]
public sealed class PersonMessage
{
    [ProtoMember(1)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    [ProtoMember(3)]
    public int Age { get; set; }
}

[QueueName("scenario.batch.metrics")]
public sealed class MetricMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    public int Value { get; set; }
}
