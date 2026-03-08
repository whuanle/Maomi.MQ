using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction;

namespace Maomi.MQ.Examples.Transaction.Api;

[RouterKey("example.transaction.order.created")]
public sealed class TransactionOrderCreatedMessage
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Consumer("example.transaction.order.created", Qos = 1)]
public sealed class TransactionOrderCreatedConsumer : IConsumer<TransactionOrderCreatedMessage>
{
    private readonly ILogger<TransactionOrderCreatedConsumer> _logger;
    private readonly ITransactionBarrierService _barrierService;

    public TransactionOrderCreatedConsumer(
        ILogger<TransactionOrderCreatedConsumer> logger,
        ITransactionBarrierService barrierService)
    {
        _logger = logger;
        _barrierService = barrierService;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, TransactionOrderCreatedMessage message)
    {
        await _barrierService.ExecuteInBarrierAsync(
            "example.transaction.order.created",
            messageHeader,
            (connection, transaction, cancellationToken) =>
            {
                _ = connection;
                _ = transaction;
                _ = cancellationToken;
                // Business DB work can be done in this delegate with the same transaction.
                _logger.LogInformation(
                    "Transaction consumer executed. HeaderId={HeaderId}, OrderNo={OrderNo}, Amount={Amount}",
                    messageHeader.Id,
                    message.OrderNo,
                    message.Amount);
                return Task.CompletedTask;
            });
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
