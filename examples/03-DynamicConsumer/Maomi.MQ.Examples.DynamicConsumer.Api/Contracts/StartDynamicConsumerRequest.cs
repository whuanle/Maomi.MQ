namespace Maomi.MQ.Examples.DynamicConsumer.Api.Contracts;

public sealed class StartDynamicConsumerRequest
{
    public string Queue { get; set; } = "example.dynamic.runtime";

    public ushort Qos { get; set; } = 1;
}
