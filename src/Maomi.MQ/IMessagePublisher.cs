using RabbitMQ.Client;

namespace Maomi.MQ
{
    public interface IMessagePublisher
    {
        Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null) where TEvent : class;
    }
}
