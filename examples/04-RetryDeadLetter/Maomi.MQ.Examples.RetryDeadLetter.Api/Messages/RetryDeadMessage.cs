using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Messages;

[RouterKey("example.retry.dead")]
public sealed class RetryDeadMessage
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset DeadAt { get; set; } = DateTimeOffset.UtcNow;
}
