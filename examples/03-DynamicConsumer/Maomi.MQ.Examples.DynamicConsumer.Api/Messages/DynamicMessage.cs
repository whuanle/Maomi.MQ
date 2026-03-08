using Maomi.MQ.Attributes;

namespace Maomi.MQ.Examples.DynamicConsumer.Api.Messages;

[RouterKey("example.dynamic.default")]
public sealed class DynamicMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text { get; set; } = string.Empty;
}
