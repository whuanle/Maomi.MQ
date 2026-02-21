using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.Mysql;
using MySqlConnector;
using RabbitMQ.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        options.WorkId = 7;
        options.AppName = "transaction-api";
        options.Rabbit = rabbit =>
        {
            rabbit.HostName = builder.Configuration["RabbitMQ:Host"]
                ?? Environment.GetEnvironmentVariable("RABBITMQ")
                ?? "127.0.0.1";
            rabbit.Port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
            rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
            rabbit.ClientProvidedName = Assembly.GetExecutingAssembly().GetName().Name;
        };
    },
    [typeof(Program).Assembly],
    Maomi.MQ.Extensions.CreateTransactionFilters());

var transactionConnectionString = builder.Configuration["TransactionDb"]
    ?? Environment.GetEnvironmentVariable("MQ_TRANSACTION_DB")
    ?? "Server=127.0.0.1;Port=3306;Database=maomi_mq;User ID=root;Password=123456;";

builder.Services.AddMaomiMQTransactionMySql();
builder.Services.AddMaomiMQTransaction(options =>
{
    options.ProviderName = TransactionProviderNames.MySql;
    options.Connection = _ => new MySqlConnection(transactionConnectionString);
    options.AutoCreateTable = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
