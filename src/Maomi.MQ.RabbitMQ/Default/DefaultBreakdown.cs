using RabbitMQ.Client.Events;

namespace Maomi.MQ.Default;

/// <summary>
/// Fault processor, which calls the relevant interface when the program fails.<br />
/// 故障处理器，当程序出现故障时会调用相关接口.
/// </summary>
public class DefaultBreakdown : IBreakdown
{
    /// <inheritdoc />
    public Task BasicReturn(object sender, BasicReturnEventArgs @event)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotFoundConsumer(string queue, Type messageType, Type consumerType)
    {
        return Task.CompletedTask;
    }
}