using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.EFCore;
using Maomi.MQ.Transaction.Models;
using Microsoft.EntityFrameworkCore;

namespace Maomi.MQ.Examples.Transaction.EFCore.Api;

public sealed class TransactionEfCoreDbContext : DbContext, ITransactionMessageDbContext
{
    private readonly IMQTransactionOptions _transactionOptions;

    public TransactionEfCoreDbContext(
        DbContextOptions<TransactionEfCoreDbContext> options,
        IMQTransactionOptions transactionOptions)
        : base(options)
    {
        _transactionOptions = transactionOptions;
    }

    public DbSet<EfDemoOrder> DemoOrders => Set<EfDemoOrder>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    public DbSet<InboxBarrierEntity> InboxBarriers => Set<InboxBarrierEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EfDemoOrder>(builder =>
        {
            builder.ToTable("demo_orders_ef");
            builder.HasKey(x => x.OrderId);
            builder.Property(x => x.OrderId).HasMaxLength(64);
            builder.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.CreateTime).IsRequired();
            builder.HasIndex(x => x.OrderNo).IsUnique();
        });

        modelBuilder.ApplyMaomiMQTransactionModel(_transactionOptions);
    }
}

public sealed class EfDemoOrder
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNo { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTimeOffset CreateTime { get; set; }
}
