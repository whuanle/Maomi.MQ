using Maomi.MQ.Defaults;
using QosPublisher.Controllers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQConsole;

public class Program
{
    public static async Task Main()
    {
        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ")!,
            Port = 5672,
            ConsumerDispatchConcurrency = 1000
        };

        var connection = await connectionFactory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: false,
            publisherConfirmationTrackingEnabled: false,
            consumerDispatchConcurrency: 1000));
        var messageSerializer = new DefaultMessageSerializer();

        var consumer = new AsyncEventingBasicConsumer(channel);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 100, global: true);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var testEvent = messageSerializer.Deserialize<TestEvent>(eventArgs.Body.Span);
            Console.WriteLine($"start time:{DateTime.Now} {testEvent.Id}");
            await Task.Delay(50);
            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(
            queue: "qos",
            autoAck: false,
            consumer: consumer);

        while (true)
        {
            await Task.Delay(10000);
        }
    }
}