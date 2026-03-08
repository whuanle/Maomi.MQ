using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.Mysql;
using MySqlConnector;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        var rabbitUri = builder.Configuration["RabbitMQ"]
            ?? Environment.GetEnvironmentVariable("RabbitMQ")
            ?? "amqp://guest:guest@127.0.0.1:5672";

        options.WorkId = 7;
        options.AppName = "transaction-api";
        options.Rabbit = rabbit =>
        {
            rabbit.Uri = new Uri(uriString: rabbitUri!);
        };
    },
    [typeof(Program).Assembly],
    f => f.AddRange(Maomi.MQ.Extensions.CreateTransactionFilters()));

var transactionConnectionString = builder.Configuration.GetConnectionString("TransactionDb")
    ?? builder.Configuration["TransactionDb"]
    ?? Environment.GetEnvironmentVariable("MQ_TRANSACTION_DB")
    ?? "Server=127.0.0.1;Port=3306;Database=maomi_mq;User ID=root;Password=123456;";

builder.Services.AddMaomiMQTransactionMySql();
builder.Services.AddMaomiMQTransaction(options =>
{
    options.ProviderName = TransactionProviderNames.MySql;
    options.Connection = _ => new MySqlConnection(transactionConnectionString);
    options.AutoCreateTable = true;
    options.Cleanup = new Maomi.MQ.Transaction.Models.MQTransactionCleanupOptions
    {
        Enabled = true,
        ScanInterval = TimeSpan.FromMinutes(2),
        KeepCompletedDays = 7,
        MaxCompletedCount = 200000,
        DeleteBatchSize = 1000
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi(c =>
    {
        c.Path = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
