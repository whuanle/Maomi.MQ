
using EventWeb.Events;
using Maomi.MQ;
using Maomi.MQ.EventBus;
using RabbitMQ.Client;
using System.Reflection;

namespace EventWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddMaomiMQ((MqOptionsBuilder options) =>
        {
            options.WorkId = 1;
            options.AppName = "myapp";
            options.Rabbit = (ConnectionFactory options) =>
            {
                options.HostName = "10.1.0.6";
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, [typeof(Program).Assembly], [new ConsumerTypeFilter(), new EventBusTypeFilter(EventTopicFilter)]);
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }


    private static bool EventTopicFilter(EventTopicAttribute eventTopicAttribute, Type eventType)
    {
        if (eventType == typeof(DynamicTestEvent))
        {
            eventTopicAttribute.Queue = eventTopicAttribute.Queue + "_1";
        }

        return true;
    }
}
