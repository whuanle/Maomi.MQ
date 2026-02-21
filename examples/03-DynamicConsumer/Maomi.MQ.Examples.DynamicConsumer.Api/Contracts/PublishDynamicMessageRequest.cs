namespace Maomi.MQ.Examples.DynamicConsumer.Api.Contracts;

public sealed class PublishDynamicMessageRequest
{
    public string Queue { get; set; } = "example.dynamic.runtime";

    public string Text { get; set; } = "hello dynamic";
}
