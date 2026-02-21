using Maomi.MQ;
using Maomi.MQ.Examples.Transaction.Api;
using Maomi.MQ.Transaction;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Maomi.MQ.Examples.Transaction.Api.Controllers;

[ApiController]
[Route("api/transaction")]
public sealed class TransactionController : ControllerBase
{
    private readonly ITransactionOutboxService _outboxService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(
        IConfiguration configuration,
        ITransactionOutboxService outboxService,
        ILogger<TransactionController> logger)
    {
        _configuration = configuration;
        _outboxService = outboxService;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than 0.");
        }

        var orderNo = string.IsNullOrWhiteSpace(request.OrderNo)
            ? $"SO-TX-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}"
            : request.OrderNo.Trim();

        var message = new TransactionOrderCreatedMessage
        {
            OrderId = Guid.NewGuid(),
            OrderNo = orderNo,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var connectionString = GetConnectionString();

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureBusinessTableAsync(connection, transaction: null, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await InsertOrderAsync(connection, transaction, message, cancellationToken);

        // RegisterAutoAsync inserts a pending outbox row into the same DB transaction.
        var outbox = await _outboxService.RegisterAutoAsync(
            connection,
            transaction,
            message,
            cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Transaction committed. OrderNo={OrderNo}, OutboxMessageId={OutboxMessageId}. The background publisher will dispatch it.",
            message.OrderNo,
            outbox.MessageId);

        return Results.Ok(new
        {
            Status = "CommittedToOutbox",
            OutboxMessageId = outbox.MessageId,
            OutboxStatus = "Pending",
            DispatchMode = "BackgroundService",
            message.OrderId,
            message.OrderNo,
            message.Amount,
            message.CreatedAt
        });
    }

    [HttpGet("orders")]
    public async Task<IResult> GetOrders([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        if (take > 200)
        {
            take = 200;
        }

        var connectionString = GetConnectionString();

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureBusinessTableAsync(connection, transaction: null, cancellationToken);

        const string querySql =
            """
            SELECT `order_id`, `order_no`, `amount`, `create_time`
            FROM `demo_orders`
            ORDER BY `create_time` DESC
            LIMIT @take;
            """;

        using var query = new MySqlCommand(querySql, connection);
        query.Parameters.AddWithValue("@take", take);

        List<DemoOrderItem> orders = new();

        await using var reader = await query.ExecuteReaderAsync(cancellationToken);
        var orderIdOrdinal = reader.GetOrdinal("order_id");
        var orderNoOrdinal = reader.GetOrdinal("order_no");
        var amountOrdinal = reader.GetOrdinal("amount");
        var createTimeOrdinal = reader.GetOrdinal("create_time");
        while (await reader.ReadAsync(cancellationToken))
        {
            var createTime = reader.GetDateTime(createTimeOrdinal);
            if (createTime.Kind == DateTimeKind.Unspecified)
            {
                createTime = DateTime.SpecifyKind(createTime, DateTimeKind.Utc);
            }

            orders.Add(new DemoOrderItem
            {
                OrderId = reader.GetString(orderIdOrdinal),
                OrderNo = reader.GetString(orderNoOrdinal),
                Amount = reader.GetDecimal(amountOrdinal),
                CreateTime = new DateTimeOffset(createTime.ToUniversalTime(), TimeSpan.Zero)
            });
        }

        return Results.Ok(orders);
    }

    [HttpGet("status")]
    public IResult GetStatus() => Results.Ok(new
    {
        Name = "transaction-api",
        Time = DateTimeOffset.UtcNow
    });

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("TransactionDb")
            ?? Environment.GetEnvironmentVariable("MQ_TRANSACTION_DB")
            ?? "Server=127.0.0.1;Port=3306;Database=maomi_mq;User ID=root;Password=123456;";
    }

    private static async Task EnsureBusinessTableAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            CREATE TABLE IF NOT EXISTS `demo_orders` (
              `order_id` varchar(64) NOT NULL,
              `order_no` varchar(64) NOT NULL,
              `amount` decimal(18,2) NOT NULL,
              `create_time` datetime(6) NOT NULL,
              PRIMARY KEY (`order_id`),
              UNIQUE KEY `ux_demo_orders_order_no` (`order_no`)
            ) ENGINE=InnoDB;
            """;

        using var command = new MySqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertOrderAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        TransactionOrderCreatedMessage message,
        CancellationToken cancellationToken)
    {
        const string insertSql =
            """
            INSERT INTO `demo_orders` (`order_id`, `order_no`, `amount`, `create_time`)
            VALUES (@order_id, @order_no, @amount, @create_time);
            """;

        using var insert = new MySqlCommand(insertSql, connection, transaction);
        insert.Parameters.AddWithValue("@order_id", message.OrderId.ToString("N"));
        insert.Parameters.AddWithValue("@order_no", message.OrderNo);
        insert.Parameters.AddWithValue("@amount", message.Amount);
        insert.Parameters.AddWithValue("@create_time", message.CreatedAt.UtcDateTime);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class PublishTransactionRequest
{
    public string? OrderNo { get; set; }

    public decimal Amount { get; set; } = 99m;
}

public sealed class DemoOrderItem
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreateTime { get; set; }
}
