using Maomi.MQ;
using Maomi.MQ.Examples.Transaction.EFCore.Api;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.EFCore;
using Microsoft.EntityFrameworkCore;
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

        options.WorkId = 8;
        options.AppName = "transaction-efcore-api";
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

builder.Services.AddDbContext<TransactionEfCoreDbContext>(options =>
{
    options.UseMySql(transactionConnectionString, ServerVersion.AutoDetect(transactionConnectionString));
});

builder.Services.AddMaomiMQTransaction(options =>
{
    // Keeps transaction runtime options and background dispatcher enabled.
    options.ProviderName = "mysql";
    options.Connection = _ => new MySqlConnector.MySqlConnection(transactionConnectionString);
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

builder.Services.AddMaomiMQTransactionEFCore<TransactionEfCoreDbContext>(
    options =>
{
    options.AutoSaveChanges = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionEfCoreDbContext>();
    await db.Database.EnsureCreatedAsync();
}

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
