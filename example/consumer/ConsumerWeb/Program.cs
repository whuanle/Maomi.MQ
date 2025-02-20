using ConsumerWeb.Consumer;
using ConsumerWeb.Models;
using Maomi.MQ;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Reflection;

namespace ActivitySourceApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole().AddDebug();

        builder.Services.AddMaomiMQ((MqOptionsBuilder options) =>
        {
            options.WorkId = 1;
            options.AutoQueueDeclare = true;
            options.AppName = "myapp";
            options.Rabbit = (ConnectionFactory options) =>
            {
                options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
                options.Port = 5672;
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, [typeof(Program).Assembly], [new ConsumerTypeFilter(), new EventBusTypeFilter(EventInterceptor),]);

        builder.Services.AddSingleton<IRetryPolicyFactory, MyDefaultRetryPolicyFactory>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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

    private static RegisterQueue ConsumerInterceptor(IConsumerOptions consumerOptions, Type consumerType)
    {
        var newConsumerOptions = new ConsumerOptions(consumerOptions.Queue);
        consumerOptions.CopyFrom(newConsumerOptions);

        // 修改 newConsumerOptions 中的配置

        return new RegisterQueue(true, consumerOptions);
    }

    private static RegisterQueue EventInterceptor(IConsumerOptions consumerOptions, Type eventType)
    {
        if (eventType == typeof(TestEvent))
        {
            var newConsumerOptions = new ConsumerOptions(consumerOptions.Queue);
            consumerOptions.CopyFrom(newConsumerOptions);
            newConsumerOptions.Queue = newConsumerOptions.Queue + "_1";

            return new RegisterQueue(true, newConsumerOptions);
        }
        return new RegisterQueue(true, consumerOptions);
    }
}
