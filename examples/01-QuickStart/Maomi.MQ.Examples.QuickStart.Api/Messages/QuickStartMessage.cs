using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.QuickStart.Api.Messages;

[RouterKey("example.quickstart")]
public sealed class QuickStartMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string Text { get; set; } = string.Empty;
}
