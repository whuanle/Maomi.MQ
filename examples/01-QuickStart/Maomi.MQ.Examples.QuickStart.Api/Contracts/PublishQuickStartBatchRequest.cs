namespace Maomi.MQ.Examples.QuickStart.Api.Contracts;

public sealed class PublishQuickStartBatchRequest
{
    public string TextPrefix { get; set; } = "hello batch";

    public int Count { get; set; } = 10;
}
