using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Samples.ScenarioHub;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<ScenarioRuntimeState>();
builder.Services.AddHostedService<BatchPublisherBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        var protobufSerializer = new ProtobufMessageSerializer();
        options.WorkId = 10;
        options.AppName = "scenario-hub-api";
        options.MessageSerializers = serializers => serializers.Insert(0, protobufSerializer);
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
