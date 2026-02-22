using Maomi.MQ.Attributes;
using Maomi.MQ.EventBus;
using Maomi.MQ.Filters;
using Maomi.MQ.Transaction;
using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Default;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maomi.MQ.Transaction.UnitTests;

public class TransactionFiltersTests
{
    [Fact]
    public void CreateTransactionFilters_ShouldReturnTwoFilters()
    {
        var filters = Extensions.CreateTransactionFilters();

        Assert.Equal(2, filters.Length);
        Assert.Contains(filters, x => x is DbTransactionTypeFilter);
        Assert.Contains(filters, x => x is EventBusTransactionTypeFilter);
    }

    [Fact]
    public void DbTransactionTypeFilter_ShouldRegisterWrappedConsumer()
    {
        var services = new ServiceCollection();
        services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));

        services.AddMaomiMQ(
            builder =>
            {
                builder.AppName = "app";
                builder.WorkId = 1;
                builder.Rabbit = f => f.HostName = "localhost";
            },
            [typeof(TransactionDbConsumer).Assembly],
            [new DbTransactionTypeFilter()]);

        services.AddSingleton<IMQTransactionOptions>(CreateOptions());
        services.AddScoped<IDatabaseProvider>(_ => new NoopDatabaseProvider());
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var consumer = scope.ServiceProvider.GetRequiredService<IConsumer<DbMessage>>();
        Assert.IsType<DbTransactionConsumer<DbMessage>>(consumer);
    }

    [Fact]
    public void EventBusTransactionTypeFilter_ShouldReplaceEventMiddlewareWithTransactionMiddleware()
    {
        var services = new ServiceCollection();
        services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));

        services.AddMaomiMQ(
            builder =>
            {
                builder.AppName = "app";
                builder.WorkId = 1;
                builder.Rabbit = f => f.HostName = "localhost";
            },
            [typeof(TransactionEventMiddlewareOnly).Assembly],
            [new EventBusTransactionTypeFilter()]);

        services.AddSingleton<IMQTransactionOptions>(CreateOptions());
        services.AddScoped<IDatabaseProvider>(_ => new NoopDatabaseProvider());
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var middleware = scope.ServiceProvider.GetRequiredService<IEventMiddleware<EventMessage>>();
        Assert.IsType<TransactionEventMiddleware<EventMessage>>(middleware);
    }

    [Consumer("tran-test-db")]
    private sealed class TransactionDbConsumer : IDbTransactionConsumer<DbMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, DbMessage message) => Task.CompletedTask;

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, DbMessage message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, DbMessage? message, Exception? ex) =>
            Task.FromResult(ConsumerState.Ack);
    }

    [Consumer("tran-test-event")]
    private sealed class TransactionEventMiddlewareOnly : IEventMiddleware<EventMessage>
    {
        public Task ExecuteAsync(MessageHeader messageHeader, EventMessage message, EventHandlerDelegate<EventMessage> next)
        {
            return next(messageHeader, message, CancellationToken.None);
        }

        public Task FaildAsync(MessageHeader messageHeader, Exception ex, int retryCount, EventMessage? message) => Task.CompletedTask;

        public Task<ConsumerState> FallbackAsync(MessageHeader messageHeader, EventMessage? message, Exception? ex) =>
            Task.FromResult(ConsumerState.Ack);
    }

    [EventOrder(1)]
    private sealed class TransactionEventHandler : IEventHandler<EventMessage>
    {
        public Task ExecuteAsync(EventMessage message, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CancelAsync(EventMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class DbMessage
    {
        public int Id { get; set; }
    }

    private sealed class EventMessage
    {
        public int Id { get; set; }
    }

    private static IMQTransactionOptions CreateOptions()
    {
        return new MQTransactionOptions
        {
            ProviderName = "mysql",
            Connection = _ => throw new NotImplementedException(),
        };
    }

    private sealed class NoopDatabaseProvider : IDatabaseProvider
    {
        public Task EnsureTablesExistAsync(System.Data.Common.DbCommand command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InsertOutboxAsync(System.Data.Common.DbCommand command, OutboxMessageEntity message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> TryLockOutboxAsync(
            System.Data.Common.DbCommand command,
            long messageId,
            string lockId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<int> TryLockOutboxBatchAsync(
            System.Data.Common.DbCommand command,
            string lockId,
            DateTimeOffset now,
            TimeSpan lockTimeout,
            int maxRetry,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<OutboxMessageEntity>> GetLockedOutboxBatchAsync(
            System.Data.Common.DbCommand command,
            string lockId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OutboxMessageEntity>>(Array.Empty<OutboxMessageEntity>());

        public Task<bool> MarkOutboxSucceededAsync(
            System.Data.Common.DbCommand command,
            long messageId,
            string lockId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> MarkOutboxFailedAsync(
            System.Data.Common.DbCommand command,
            long messageId,
            string lockId,
            DateTimeOffset now,
            DateTimeOffset nextRetryTime,
            string errorMessage,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<InboxBarrierEnterResult> TryEnterInboxBarrierAsync(
            System.Data.Common.DbCommand command,
            InboxBarrierEntity barrier,
            TimeSpan lockTimeout,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(InboxBarrierEnterResult.Entered);

        public Task<bool> MarkInboxBarrierSucceededAsync(
            System.Data.Common.DbCommand command,
            string consumerName,
            long messageId,
            string lockId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> MarkInboxBarrierFailedAsync(
            System.Data.Common.DbCommand command,
            string consumerName,
            long messageId,
            string lockId,
            DateTimeOffset now,
            string errorMessage,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<long> CountSucceededOutboxAsync(System.Data.Common.DbCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<long> CountSucceededInboxAsync(System.Data.Common.DbCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<int> DeleteSucceededOutboxBeforeAsync(
            System.Data.Common.DbCommand command,
            DateTimeOffset cutoffTime,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> DeleteSucceededInboxBeforeAsync(
            System.Data.Common.DbCommand command,
            DateTimeOffset cutoffTime,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> DeleteOldestSucceededOutboxAsync(
            System.Data.Common.DbCommand command,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> DeleteOldestSucceededInboxAsync(
            System.Data.Common.DbCommand command,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
