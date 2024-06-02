using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Maomi.MQ;
using OpenTelemetry.Exporter;

namespace ActivitySourceApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string serviceName = "roll-dice";

        builder.Services.AddOpenTelemetry()
              .ConfigureResource(resource => resource.AddService(serviceName))
              .WithTracing(tracing =>
              {
                  tracing.AddMaomiMQInstrumentation(options =>
                  {
                      options.RecordException = true;
                  })
                  .AddAspNetCoreInstrumentation()
                  .AddOtlpExporter(options =>
                  {
                      options.Endpoint = new Uri("http://20.189.120.90:32808/v1/traces");
                      options.Protocol = OtlpExportProtocol.HttpProtobuf;
                  });
              });

        builder.Services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
        }, options =>
        {
            options.HostName = "192.168.1.4";
            options.ClientProvidedName = "aaa";
        }, new System.Reflection.Assembly[] { typeof(Program).Assembly });



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
