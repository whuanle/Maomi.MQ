using Maomi.MQ;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.MediatR;
using RabbitMQ.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblies(new Assembly[]
    {
        Assembly.GetExecutingAssembly(),
        typeof(MediatrTypeFilter).Assembly
    });

    options.RegisterGenericHandlers = true;
});

builder.Services.AddMaomiMQ((MqOptionsBuilder options) =>
{
    options.WorkId = 1;
    options.AutoQueueDeclare = true;
    options.AppName = "myapp";
    options.Rabbit = (ConnectionFactory options) =>
    {
        options.HostName = Environment.GetEnvironmentVariable("RABBITMQ")!;
        options.Port = 5672;
        options.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
    };
}, [typeof(Program).Assembly], [new ConsumerTypeFilter(), new EventBusTypeFilter(), new MediatrTypeFilter()]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
