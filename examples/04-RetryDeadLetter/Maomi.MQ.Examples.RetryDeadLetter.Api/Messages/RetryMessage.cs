using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.RetryDeadLetter.Api.Messages;

[RouterKey("example.retry.main")]
public sealed class RetryMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;

    public bool ForceFail { get; set; } = true;
}
