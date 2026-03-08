using Maomi.MQ.Transaction.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maomi.MQ.Examples.Transaction.EFCore.Api.Controllers;

[ApiController]
[Route("api/transaction-ef")]
public sealed class TransactionEfCoreController : ControllerBase
{
    private readonly TransactionEfCoreDbContext _dbContext;
    private readonly ITransactionMessageDbContext _transactionDbContext;
    private readonly IEfCoreTransactionService _efCoreTransactionService;

    public TransactionEfCoreController(
        TransactionEfCoreDbContext dbContext,
        ITransactionMessageDbContext transactionDbContext,
        IEfCoreTransactionService efCoreTransactionService)
    {
        _dbContext = dbContext;
        _transactionDbContext = transactionDbContext;
        _efCoreTransactionService = efCoreTransactionService;
    }

    [HttpPost("publish")]
    public async Task<IResult> Publish([FromBody] PublishTransactionEfRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than 0.");
        }

        var orderNo = string.IsNullOrWhiteSpace(request.OrderNo)
            ? $"SO-EF-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}"
            : request.OrderNo.Trim();

        var message = new TransactionEfOrderCreatedMessage
        {
            OrderId = Guid.NewGuid(),
            OrderNo = orderNo,
            Amount = request.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var order = new EfDemoOrder
        {
            OrderId = message.OrderId.ToString("N"),
            OrderNo = message.OrderNo,
            Amount = message.Amount,
            CreateTime = message.CreatedAt
        };

        var outbox = await _efCoreTransactionService.ExecuteAndRegisterAsync(
            _dbContext,
            async (db, ct) =>
            {
                db.Set<EfDemoOrder>().Add(order);
                await Task.CompletedTask;
            },
            string.Empty,
            "example.transaction.efcore.order.created",
            message,
            cancellationToken: cancellationToken);

        await outbox.PublishAsync(cancellationToken);

        return Results.Ok(new
        {
            Status = "CommittedToOutbox",
            OutboxMessageId = outbox.MessageId,
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

        var orders = await _dbContext.DemoOrders
            .AsNoTracking()
            .OrderByDescending(x => x.CreateTime)
            .Take(take)
            .Select(x => new EfDemoOrderItem
            {
                OrderId = x.OrderId,
                OrderNo = x.OrderNo,
                Amount = x.Amount,
                CreateTime = x.CreateTime
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(orders);
    }

    [HttpGet("status")]
    public IResult GetStatus() => Results.Ok(new
    {
        Name = "transaction-efcore-api",
        Time = DateTimeOffset.UtcNow
    });

    [HttpGet("outbox")]
    public async Task<IResult> GetOutbox([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        if (take > 200)
        {
            take = 200;
        }

        var outbox = await _transactionDbContext.OutboxMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreateTime)
            .Take(take)
            .Select(x => new
            {
                x.MessageId,
                x.Exchange,
                x.RoutingKey,
                x.Status,
                x.RetryCount,
                x.NextRetryTime,
                x.LastError,
                x.CreateTime,
                x.UpdateTime
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(outbox);
    }

    [HttpGet("inbox")]
    public async Task<IResult> GetInbox([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 20;
        }

        if (take > 200)
        {
            take = 200;
        }

        var inbox = await _transactionDbContext.InboxBarriers
            .AsNoTracking()
            .OrderByDescending(x => x.CreateTime)
            .Take(take)
            .Select(x => new
            {
                x.ConsumerName,
                x.MessageId,
                x.Exchange,
                x.RoutingKey,
                x.Status,
                x.LastError,
                x.CreateTime,
                x.UpdateTime
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(inbox);
    }
}

public sealed class PublishTransactionEfRequest
{
    public string? OrderNo { get; set; }

    public decimal Amount { get; set; } = 99m;
}

public sealed class EfDemoOrderItem
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreateTime { get; set; }
}
