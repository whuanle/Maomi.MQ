using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction;
using Maomi.MQ.Transaction.Database;
using MySqlConnector;

namespace Maomi.MQ.Examples.Transaction.Api;

[RouterKey("example.transaction.order.created")]
public sealed class TransactionOrderCreatedMessage
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Consumer(TransactionOrderCreatedConsumer.QueueName, Qos = 1)]
public sealed class TransactionOrderCreatedConsumer : IConsumer<TransactionOrderCreatedMessage>
{
    public const string QueueName = "example.transaction.order.created";

    private readonly ILogger<TransactionOrderCreatedConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITransactionBarrierService _barrierService;

    public TransactionOrderCreatedConsumer(
        ILogger<TransactionOrderCreatedConsumer> logger,
        IConfiguration configuration,
        ITransactionBarrierService barrierService)
    {
        _logger = logger;
        _configuration = configuration;
        _barrierService = barrierService;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, TransactionOrderCreatedMessage message)
    {
        var connectionString = _configuration.GetConnectionString("TransactionDb")
            ?? Environment.GetEnvironmentVariable("MQ_TRANSACTION_DB")
            ?? "Server=127.0.0.1;Port=3306;Database=maomi_mq;User ID=root;Password=123456;";

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var barrier = await _barrierService.EnterAsync(connection, transaction, QueueName, messageHeader);

        if (barrier.EnterResult != InboxBarrierEnterResult.Entered)
        {
            await transaction.CommitAsync();
            _logger.LogInformation(
                "Transaction consumer skipped. HeaderId={HeaderId}, OrderNo={OrderNo}, EnterResult={EnterResult}",
                messageHeader.Id,
                message.OrderNo,
                barrier.EnterResult);
            return;
        }

        // Manual mode: user can do business DB updates here when EnterResult is Entered.
        _logger.LogInformation(
            "Transaction consumer executed. HeaderId={HeaderId}, OrderNo={OrderNo}, Amount={Amount}",
            messageHeader.Id,
            message.OrderNo,
            message.Amount);

        var updated = await _barrierService.MarkSucceededAsync(connection, transaction, barrier);
        if (!updated)
        {
            throw new InvalidOperationException($"Failed to mark inbox barrier succeeded for queue [{QueueName}] message [{messageHeader.Id}].");
        }

        await transaction.CommitAsync();
    }

    public Task FaildAsync(
        MessageHeader messageHeader,
        Exception ex,
        int retryCount,
        TransactionOrderCreatedMessage message)
    {
        _logger.LogWarning(
            ex,
            "Transaction consumer failed. HeaderId={HeaderId}, RetryCount={RetryCount}, OrderNo={OrderNo}",
            messageHeader.Id,
            retryCount,
            message.OrderNo);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(
        MessageHeader messageHeader,
        TransactionOrderCreatedMessage? message,
        Exception? ex)
    {
        _logger.LogError(
            ex,
            "Transaction consumer fallback reached. HeaderId={HeaderId}, OrderNo={OrderNo}",
            messageHeader.Id,
            message?.OrderNo);
        return Task.FromResult(ConsumerState.Ack);
    }
}
