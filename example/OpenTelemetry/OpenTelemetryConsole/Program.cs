using Maomi.MQ;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace OpenTelemetryConsole;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string serviceName = "myapp";

        builder.Services.AddMaomiMQ((options) =>
        {
            options.WorkId = 1;
            options.AutoQueueDeclare = true;
            options.AppName = serviceName;
            options.Rabbit = (options) =>
            {
                options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
                options.Port = 5672;
                options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
                options.ConsumerDispatchConcurrency = 1000;
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
                  .AddOtlpExporter("trace", options =>
                  {
                      options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTLPEndpoint")! + "/v1/traces");
                      options.Protocol = OtlpExportProtocol.HttpProtobuf;
                  });
              })
              .WithMetrics(metrices =>
              {
                  metrices.AddAspNetCoreInstrumentation()
                  .AddMaomiMQInstrumentation()
                  .AddOtlpExporter("metrics", options =>
                  {
                      options.Endpoint = new Uri(Environment.GetEnvironmentVariable("OTLPEndpoint")! + "/v1/metrics");
                      options.Protocol = OtlpExportProtocol.HttpProtobuf;
                  });
              });

        builder.Services.AddHostedService<MyPublishAsync>();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        var app = builder.Build();

        app.UseAuthorization();

        app.MapPrometheusScrapingEndpoint();

        app.MapControllers();

        app.Run();
    }
}
