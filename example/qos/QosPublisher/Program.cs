using Maomi.MQ;
using RabbitMQ.Client;
using System.Reflection;

namespace QosPublisher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddDebug();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddMaomiMQ((options) =>
        {
            options.WorkId = 1;
            options.AppName = "myapp";
            options.Rabbit = (options) =>
            {
                options.HostName = "192.168.3.248";
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, [typeof(Program).Assembly]);

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
