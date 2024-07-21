using ConsumerWeb.Consumer;
using ConsumerWeb.Models;
using Maomi.MQ;
using RabbitMQ.Client;
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
                options.HostName = "192.168.3.248";
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, AppDomain.CurrentDomain.GetAssemblies());

        builder.Services.AddTransient<IConsumer<TestEvent>, MyConsumer>();
        
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

}