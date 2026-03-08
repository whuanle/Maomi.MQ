# Maomi.MQ.Transaction + EF Core 使用说明

本文给出在 `Maomi.MQ.Transaction` 中使用 EF Core 的推荐方式，目标是避免手工 SQL 事务拼接，让业务写入和 Outbox 写入在同一个本地事务内完成。

## 1. 安装依赖

按你的数据库类型安装事务模块和 Provider 包（以下以 MySQL 为例）：

```bash
dotnet add package Maomi.MQ.Transaction
dotnet add package Maomi.MQ.Transaction.Mysql
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

如果你用 SQL Server/PostgreSQL，请替换为：

- `Maomi.MQ.Transaction.SqlServer`
- `Maomi.MQ.Transaction.Postgres`

## 2. 服务注册

在 `Program.cs` 中同时注册 MQ、EF Core 和事务模块。

```csharp
using Maomi.MQ;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction.Mysql;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var transactionDb = builder.Configuration.GetConnectionString("TransactionDb")
    ?? "Server=127.0.0.1;Port=3306;Database=maomi_mq;User ID=root;Password=123456;";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(transactionDb, ServerVersion.AutoDetect(transactionDb));
});

builder.Services.AddMaomiMQ(
    (MqOptionsBuilder options) =>
    {
        options.AppName = "demo-efcore";
        options.WorkId = 1;
        options.Rabbit = rabbit =>
        {
            rabbit.HostName = "127.0.0.1";
            rabbit.Port = 5672;
            rabbit.UserName = "guest";
            rabbit.Password = "guest";
        };
    },
    [typeof(Program).Assembly],
    Maomi.MQ.Extensions.CreateTransactionFilters());

builder.Services.AddMaomiMQTransactionMySql();
builder.Services.AddMaomiMQTransaction(options =>
{
    options.ProviderName = TransactionProviderNames.MySql;
    options.Connection = _ => new MySqlConnection(transactionDb);
    options.AutoCreateTable = true;
});
```

## 3. 发布端（EF Core + Outbox）

核心原则：

- 业务数据 `SaveChanges` 和 `RegisterAutoAsync` 必须放在同一个数据库事务里。
- `RegisterAutoAsync` 要使用 EF Core 当前事务对应的 `DbConnection`/`DbTransaction`。

```csharp
using Maomi.MQ.Transaction;
using Microsoft.EntityFrameworkCore;

public sealed class OrderAppService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionOutboxService _outboxService;

    public OrderAppService(AppDbContext dbContext, ITransactionOutboxService outboxService)
    {
        _dbContext = dbContext;
        _outboxService = outboxService;
    }

    public async Task<string> CreateOrderAsync(CreateOrderInput input, CancellationToken ct)
    {
        var message = new OrderCreatedMessage
        {
            OrderId = Guid.NewGuid(),
            OrderNo = input.OrderNo,
            Amount = input.Amount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);

        _dbContext.Orders.Add(new OrderEntity
        {
            Id = message.OrderId,
            OrderNo = message.OrderNo,
            Amount = message.Amount,
            CreateTime = message.CreatedAt
        });
        await _dbContext.SaveChangesAsync(ct);

        var dbConnection = _dbContext.Database.GetDbConnection();
        var dbTransaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("EF Core transaction is required.");

        var outbox = await _outboxService.RegisterAutoAsync(
            dbConnection,
            dbTransaction,
            message,
            cancellationToken: ct);

        await tx.CommitAsync(ct);
        return outbox.MessageId;
    }
}
```

提交后由 `PublisherBackgroundService` 扫描 `mq_publisher` 并投递消息，不需要手工调用 MQ 发送。

## 4. 消费端（Inbox Barrier + 幂等）

消费端建议使用 `ITransactionBarrierService`。当同一条消息重复投递时，Barrier 会拒绝再次执行业务逻辑，从而实现幂等。

```csharp
using Maomi.MQ;
using Maomi.MQ.Attributes;
using Maomi.MQ.Models;
using Maomi.MQ.Transaction;
using Maomi.MQ.Transaction.Database;

[Consumer("demo.order.created", Qos = 1)]
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedMessage>
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionBarrierService _barrierService;

    public OrderCreatedConsumer(AppDbContext dbContext, ITransactionBarrierService barrierService)
    {
        _dbContext = dbContext;
        _barrierService = barrierService;
    }

    public async Task ExecuteAsync(MessageHeader header, OrderCreatedMessage message)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        var connection = _dbContext.Database.GetDbConnection();
        var dbTx = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("EF Core transaction is required.");

        var barrier = await _barrierService.EnterAsync(connection, dbTx, "demo.order.created", header);
        if (barrier.EnterResult != InboxBarrierEnterResult.Entered)
        {
            await tx.CommitAsync();
            return;
        }

        // 业务处理
        // ...
        await _dbContext.SaveChangesAsync();

        var ok = await _barrierService.MarkSucceededAsync(connection, dbTx, barrier);
        if (!ok)
        {
            throw new InvalidOperationException("MarkSucceeded failed.");
        }

        await tx.CommitAsync();
    }
}
```

## 5. 注意事项

- `Connection` 委托必须返回与你 EF Core 使用的同类型数据库连接（如 `MySqlConnection`）。
- 如果关闭 `AutoCreateTable`，请提前执行 `asserts/transaction-*-default-create-table.sql`。
- `Publisher.TableName`、`Consumer.TableName` 支持改名；改名后请同步调整 DDL。
- 生产环境建议设置：
  - `Publisher.DisplayMessageText = false`（减少明文消息落库）
  - 合理的 `MaxRetry`、`LockTimeout`、`ProcessingTimeout`
- 失败排查重点：
  - `mq_publisher.last_error`
  - `mq_consumer.last_error`
  - MQ 连接配置和消费者是否在线

