using Maomi.MQ;
using Maomi.MQ.Diagnostics;
using QosPublisher.Controllers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Data.Common;
using System.Threading.Channels;
class Program
{
    static async Task Main()
    {
        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = "10.1.0.4"
        };

        var connection = await connectionFactory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var consumer = new EventingBasicConsumer(channel);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 30, global: true);

        consumer.Received += async (sender, eventArgs) =>
        {
            Dictionary<string, object> loggerState = new() { { DiagnosticName.Activity.Consumer, "Queue" } };
            if (eventArgs.BasicProperties.Headers?.TryGetValue(DiagnosticName.Event.Id, out var eventId) == true)
            {
                loggerState.Add(DiagnosticName.Event.Id, eventId!);
            }

            var testEvent = System.Text.Json.JsonSerializer.Deserialize<EventBody<TestEvent>>(eventArgs.Body.Span);
            Console.WriteLine($"start time:{DateTime.Now} {testEvent.Body.Id}");
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