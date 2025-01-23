using RabbitMQ.Client.Events;

namespace Maomi.MQ;

public interface IBreakdown
{
    Task NotFoundConsumer(string queue, Type messageType, Type consumerType);

    Task BasicReturn(object sender, BasicReturnEventArgs @event);
}

public class DefaultBreakdown : IBreakdown
{
    // 处理不可路由消息
    public Task BasicReturn(object sender, BasicReturnEventArgs @event)
    {
        return Task.CompletedTask;
    }

    public Task NotFoundConsumer(string queue, Type messageType, Type consumerType)
    {
        return Task.CompletedTask;
    }
}