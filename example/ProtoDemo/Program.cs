using Maomi.MQ;
using Maomi.MQ.Default;
using Maomi.MQ.Defaults;
using System.Reflection;

namespace ProtoDemo;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string serviceName = "myapp";

        builder.Services.AddMaomiMQ((options) =>
        {
            options.WorkId = 1;
            options.AutoQueueDeclare = true;
            options.AppName = serviceName;
            options.Rabbit = (options) =>
            {
                options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
                options.Port = 5672;
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
                options.ConsumerDispatchConcurrency = 1000;
            };
        }, [typeof(Program).Assembly]);

        builder.Services.AddSingleton<IMessageSerializer, MessageSerializerFactory>(s =>
        {
            return new MessageSerializerFactory((type) =>
            {
                if (type.IsAssignableTo(typeof(Google.Protobuf.IMessage)))
                {
                    return new ProtobufMessageSerializer();
                }
                else
                {
                    return new DefaultMessageSerializer();
                }
            }, (type, messageHeader) =>
            {
                if (type.IsAssignableTo(typeof(Google.Protobuf.IMessage)) || messageHeader.ContentType == "application/x-protobuf")
                {
                    return new ProtobufMessageSerializer();
                }
                else
                {
                    return new DefaultMessageSerializer();
                }
            });
        });

        builder.Services.AddHostedService<MyPublishAsync>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        var app = builder.Build();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
