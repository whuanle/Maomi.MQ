
using Maomi.MQ;
using RabbitMQ.Client;
using System.Reflection;

namespace PublisherWeb;

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

        builder.Services.AddMaomiMQ((MqOptions options) =>
        {
            options.WorkId = 1;
        }, (ConnectionFactory options) =>
        {
            options.HostName = "192.168.1.4";
            options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
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
    }
}
