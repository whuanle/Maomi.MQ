using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Maomi.MQ;

namespace ActivitySourceApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        const string serviceName = "roll-dice";

        builder.Logging.AddOpenTelemetry(options =>
        {
            options
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName))
                .AddConsoleExporter();
        });
        builder.Services.AddOpenTelemetry()
              .ConfigureResource(resource => resource.AddService(serviceName))
              .WithTracing(tracing =>
              {
                  tracing.AddMaomiMQInstrumentation(options =>
                  {
                      options.RecordException = true;
                  })
                  .AddAspNetCoreInstrumentation()
                  .AddConsoleExporter();
              })
              .WithMetrics(metrics => metrics
                  .AddAspNetCoreInstrumentation()
                  .AddConsoleExporter());

        builder.Services.AddMaomiMQ(options =>
        {
            options.WorkId = 1;
        }, options =>
        {
            options.HostName = "192.168.1.4";
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
