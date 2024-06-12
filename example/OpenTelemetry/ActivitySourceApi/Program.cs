using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Maomi.MQ;
using OpenTelemetry.Exporter;
using RabbitMQ.Client;
using System.Reflection;

namespace ActivitySourceApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string serviceName = "myapp";

        builder.Services.AddMaomiMQ((MqOptionsBuilder options) =>
        {
            options.WorkId = 1;
            options.AutoQueueDeclare = true;
            options.AppName = serviceName;
            options.Rabbit = (ConnectionFactory options) =>
            {
                options.HostName = "192.168.3.248";
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
            };
        }, [typeof(Program).Assembly]);

        builder.Services.AddOpenTelemetry()
              .ConfigureResource(resource => resource.AddService(serviceName))
              .WithTracing(tracing =>
              {
                  tracing.AddMaomiMQInstrumentation(options =>
                  {
                      options.Sources.AddRange(MaomiMQDiagnostic.Sources);
                      options.RecordException = true;
                  })
                  .AddAspNetCoreInstrumentation()
                  .AddOtlpExporter(options =>
                  {
                      options.Endpoint = new Uri("http://127.0.0.1:4318/v1/traces");
                      options.Protocol = OtlpExportProtocol.HttpProtobuf;
                  });
              });

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
