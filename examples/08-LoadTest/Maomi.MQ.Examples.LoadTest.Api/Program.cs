using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Samples.LoadTest;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        var rabbitUri = Environment.GetEnvironmentVariable("RabbitMQ") ?? builder.Configuration["RabbitMQ"];

        options.WorkId = 8;
        options.AppName = "loadtest-api";
        options.MessageSerializers = serializers =>
        {
            serializers.Insert(0, new RawBinaryMessageSerializer());
            serializers.Insert(0, new MessagePackSerializer());
            serializers.Insert(0, new ProtobufMessageSerializer());
        };
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
