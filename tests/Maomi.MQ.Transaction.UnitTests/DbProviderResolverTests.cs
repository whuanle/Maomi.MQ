using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Transaction.UnitTests;

public class DbProviderResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnProvider_WhenProviderNameMatches()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMQTransactionOptions>(CreateOptions("mysql"));
        services.AddScoped<IDatabaseProviderNamed, TestDatabaseProvider>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var resolved = DbProviderResolver.Resolve(scope.ServiceProvider);

        Assert.IsType<TestDatabaseProvider>(resolved);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenProviderNameDoesNotMatch()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMQTransactionOptions>(CreateOptions("postgres"));
        services.AddScoped<IDatabaseProviderNamed, TestDatabaseProvider>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Throws<InvalidOperationException>(() => DbProviderResolver.Resolve(scope.ServiceProvider));
    }

    private static IMQTransactionOptions CreateOptions(string providerName)
    {
        return new MQTransactionOptions
        {
            ProviderName = providerName,
            Connection = _ => throw new NotImplementedException(),
        };
    }

    private sealed class TestDatabaseProvider : IDatabaseProvider, IDatabaseProviderNamed
    {
        public string ProviderName => "mysql";

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
