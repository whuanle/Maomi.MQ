// <copyright file="EfCoreTransactionServiceCollectionExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Maomi.MQ.Transaction.EFCore;
using Maomi.MQ.Transaction.EFCore.Default;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ;

/// <summary>
/// Extensions for adding EF Core transaction integration services.
/// </summary>
public static class EfCoreTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Adds EF Core based transaction integration service with default message DbContext.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="messageDbContextOptions">Message DbContext options builder callback.</param>
    /// <param name="configure">Options configure delegate.</param>
    /// <param name="contextLifetime">Context lifetime.</param>
    /// <param name="optionsLifetime">Options lifetime.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddMaomiMQTransactionEFCore(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> messageDbContextOptions,
        Action<EfCoreTransactionOptions>? configure = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
    {
        return services.AddMaomiMQTransactionEFCore<MaomiMQTransactionDbContext>(
            messageDbContextOptions,
            configure,
            contextLifetime,
            optionsLifetime);
    }

    /// <summary>
    /// Adds EF Core based transaction integration service with custom message DbContext.
    /// </summary>
    /// <typeparam name="TMessageDbContext">Message DbContext type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="messageDbContextOptions">Message DbContext options builder callback.</param>
    /// <param name="configure">Options configure delegate.</param>
    /// <param name="contextLifetime">Context lifetime.</param>
    /// <param name="optionsLifetime">Options lifetime.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddMaomiMQTransactionEFCore<TMessageDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> messageDbContextOptions,
        Action<EfCoreTransactionOptions>? configure = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TMessageDbContext : DbContext, ITransactionMessageDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageDbContextOptions);

        services.AddDbContext<TMessageDbContext>(messageDbContextOptions, contextLifetime, optionsLifetime);
        services.AddScoped<ITransactionMessageDbContext>(s => s.GetRequiredService<TMessageDbContext>());
        RegisterCoreServices(services, configure);
        return services;
    }

    /// <summary>
    /// Adds EF Core based transaction integration service by reusing an already-registered message DbContext.
    /// </summary>
    /// <typeparam name="TMessageDbContext">Message DbContext type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Options configure delegate.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddMaomiMQTransactionEFCore<TMessageDbContext>(
        this IServiceCollection services,
        Action<EfCoreTransactionOptions>? configure = null)
        where TMessageDbContext : DbContext, ITransactionMessageDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ITransactionMessageDbContext>(s => s.GetRequiredService<TMessageDbContext>());
        RegisterCoreServices(services, configure);
        return services;
    }

    private static void RegisterCoreServices(
        IServiceCollection services,
        Action<EfCoreTransactionOptions>? configure)
    {
        EfCoreTransactionOptions options = new();
        configure?.Invoke(options);

        services.AddScoped<Maomi.MQ.Transaction.Mysql.MySqlTransactionDatabaseProvider>();
        services.AddScoped<Maomi.MQ.Transaction.Postgres.PostgresTransactionDatabaseProvider>();
        services.AddScoped<Maomi.MQ.Transaction.SqlServer.SqlServerTransactionDatabaseProvider>();
        services.AddScoped<IDatabaseProviderNamed>(s => s.GetRequiredService<Maomi.MQ.Transaction.Mysql.MySqlTransactionDatabaseProvider>());
        services.AddScoped<IDatabaseProviderNamed>(s => s.GetRequiredService<Maomi.MQ.Transaction.Postgres.PostgresTransactionDatabaseProvider>());
        services.AddScoped<IDatabaseProviderNamed>(s => s.GetRequiredService<Maomi.MQ.Transaction.SqlServer.SqlServerTransactionDatabaseProvider>());
        services.AddSingleton(options);
        services.AddScoped<IEfCoreTransactionService, EfCoreTransactionService>();
    }
}
