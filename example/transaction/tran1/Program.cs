using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.Database;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using RabbitMQ.Client;
using System.Reflection;
using tran1.DB;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddDebug();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MyDbContext>(
    o => o.UseMySql(builder.Configuration["ConnectionStrings:DefaultConnection"], new MySqlServerVersion("8.0.0"), o =>
    {
    }));

using (var scope = builder.Services.BuildServiceProvider()!.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    db.Database.EnsureCreated();
}

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

builder.Services.AddMaomiMQTransaction(o =>
{
    o.Connection = s =>
    {
        var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
        return new MySqlConnection(connectionString);
    };

    o.AutoCreateTable = true;

    o.Publisher = new Maomi.MQ.Transaction.Models.MQPublisherTransactionOptions
    {
        DisplayMessageText = true,
        ScanDbInterval = TimeSpan.FromSeconds(1),
        MaxRetry = 5,
        TableName = "mq_publisher"
    };
});

builder.Services.AddSingleton<IDatabaseProvider, MysqlProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();