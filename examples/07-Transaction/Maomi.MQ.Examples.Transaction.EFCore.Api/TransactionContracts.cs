using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Maomi.MQ.Examples.Transaction.EFCore.Api;

[RouterKey("example.transaction.efcore.order.created")]
public sealed class TransactionEfOrderCreatedMessage
{
    public Guid OrderId { get; set; } = Guid.NewGuid();

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Consumer("example.transaction.efcore.order.created", Qos = 1)]
public sealed class TransactionEfOrderCreatedConsumer : IConsumer<TransactionEfOrderCreatedMessage>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionEfOrderCreatedConsumer> _logger;

    public TransactionEfOrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionEfOrderCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(MessageHeader messageHeader, TransactionEfOrderCreatedMessage message)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var efTransactionService = scope.ServiceProvider.GetRequiredService<IEfCoreTransactionService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionEfCoreDbContext>();

        await efTransactionService.ExecuteInBarrierAsync(
            dbContext,
            "example.transaction.efcore.order.created",
            messageHeader,
            async (db, cancellationToken) =>
            {
                _ = db;
                _ = cancellationToken;

                _logger.LogInformation(
                    "EFCore transaction consumer executed. HeaderId={HeaderId}, OrderNo={OrderNo}, Amount={Amount}",
                    messageHeader.Id,
                    message.OrderNo,
                    message.Amount);

                await Task.CompletedTask;
            });
    }

    public Task FaildAsync(
        MessageHeader messageHeader,
        Exception ex,
        int retryCount,
        TransactionEfOrderCreatedMessage message)
    {
        _logger.LogWarning(
            ex,
            "EFCore transaction consumer failed. HeaderId={HeaderId}, RetryCount={RetryCount}, OrderNo={OrderNo}",
            messageHeader.Id,
            retryCount,
            message.OrderNo);
        return Task.CompletedTask;
    }

    public Task<ConsumerState> FallbackAsync(
        MessageHeader messageHeader,
        TransactionEfOrderCreatedMessage? message,
        Exception? ex)
    {
        _logger.LogError(
            ex,
            "EFCore transaction consumer fallback reached. HeaderId={HeaderId}, OrderNo={OrderNo}",
            messageHeader.Id,
            message?.OrderNo);
        return Task.FromResult(ConsumerState.Ack);
    }
}
