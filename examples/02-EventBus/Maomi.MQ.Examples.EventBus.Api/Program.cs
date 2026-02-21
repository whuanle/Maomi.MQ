using Maomi.MQ;
using Maomi.MQ.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        // amqp://guest:guest@127.0.0.1:5672
        var rabbitUri = Environment.GetEnvironmentVariable("RabbitMQ") ?? builder.Configuration["RabbitMQ"];

        options.WorkId = 2;
        options.AppName = "eventbus-api";
        options.Rabbit = rabbit =>
        {
            rabbit.Uri = new Uri(uriString: rabbitUri!);
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
