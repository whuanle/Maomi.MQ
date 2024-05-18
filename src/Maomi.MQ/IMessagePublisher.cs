using RabbitMQ.Client;

namespace Maomi.MQ
{
    /// <summary>
    /// 消息发布者.
    /// </summary>
    public interface IMessagePublisher
    {
        /// <summary>
        /// 发布消息.
        /// </summary>
        /// <typeparam name="TEvent">事件模型类.</typeparam>
        /// <param name="queue">队列名称.</param>
        /// <param name="message">事件对象.</param>
        /// <param name="properties">RabbitMQ 消息属性.</param>
        /// <returns></returns>
        Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null)
            where TEvent : class;


        Task PublishAsync<TEvent>(string queue, TEvent message, BasicProperties properties);
    }
}
