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
        // amqp://guest:guest@127.0.0.1:5672
        var rabbitUri = Environment.GetEnvironmentVariable("RabbitMQ") ?? builder.Configuration["RabbitMQ"];

        options.WorkId = 7;
        options.AppName = "transaction-api";
        options.Rabbit = rabbit =>
        {
            rabbit.Uri = new Uri(uriString: rabbitUri!);
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
    app.UseOpenApi(c =>
    {
        c.Path = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

app.MapControllers();
app.Run();
