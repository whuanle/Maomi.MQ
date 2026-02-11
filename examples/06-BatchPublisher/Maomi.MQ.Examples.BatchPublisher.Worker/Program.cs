using Maomi.MQ;
using Maomi.MQ.Examples.BatchPublisher.Worker;
using Maomi.MQ.Models;
using RabbitMQ.Client;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        options.WorkId = 6;
        options.AppName = "batch-publisher-worker";
        options.Rabbit = rabbit =>
        {
            rabbit.HostName = builder.Configuration["RabbitMQ:Host"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ")
                ?? "127.0.0.1";
            rabbit.Port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
            rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            rabbit.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
        };
    },
    [typeof(Program).Assembly]);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
