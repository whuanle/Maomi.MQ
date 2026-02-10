using Maomi.MQ;
using Maomi.MQ.Models;
using RabbitMQ.Client;
using System.Reflection;

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
        options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
        options.Port = 5672;
        options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
    };
}, [typeof(Program).Assembly]);

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
