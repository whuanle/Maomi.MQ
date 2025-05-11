using FastEndpoints;
using Maomi.MQ;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Polly;
using RabbitMQ.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddFastEndpoints(options =>
{
    options.Assemblies = new Assembly[] { Assembly.GetEntryAssembly()!, typeof(FastEndpointsTypeFilter).Assembly };

});


builder.Services.AddMaomiMQ((MqOptionsBuilder options) =>
{
    options.WorkId = 1;
    options.AutoQueueDeclare = true;
    options.AppName = "myapp";
    options.Rabbit = (ConnectionFactory options) =>
    {
        options.HostName = "192.168.50.199";//Environment.GetEnvironmentVariable("RABBITMQ")!;
        options.Port = 5672;
        options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
    };
}, [typeof(Program).Assembly], [new ConsumerTypeFilter(), new EventBusTypeFilter(), new FastEndpointsTypeFilter()]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Services.RegisterGenericCommand(typeof(FeMQCommand<>), typeof(FastEndpointMQCommandHandler<>));

app.UseFastEndpoints();
app.UseAuthorization();

app.MapControllers();

app.Run();
