using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Samples.ScenarioHub;
using Polly;
using RabbitMQ.Client;
using Scalar.AspNetCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddSingleton<BatchPublisherBackgroundService>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<BatchPublisherBackgroundService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApiDocument();

builder.Services.AddMaomiMQ(
(MqOptionsBuilder options) =>
{
    // amqp://guest:guest@127.0.0.1:5672
    var rabbitUri = Environment.GetEnvironmentVariable("RabbitMQ") ?? builder.Configuration["RabbitMQ"];

    var protobufSerializer = new ProtobufMessageSerializer();
    options.WorkId = 10;
    options.AppName = "scenario-hub-api";
    options.MessageSerializers = serializers => serializers.Insert(0, protobufSerializer);
    options.Rabbit = rabbit =>
    {
        rabbit.Uri = new Uri(rabbitUri!);
    };
},
[typeof(Program).Assembly]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(c =>
    {
        c.Path = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
