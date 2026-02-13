// <copyright file="Extensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ;

/// <summary>
/// PostgreSQL transaction extension.
/// </summary>
public static class MaomiMqTransactionPostgresExtensions
{
    /// <summary>
    /// Registers PostgreSQL transaction database provider.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddMaomiMQTransactionPostgres(this IServiceCollection services)
    {
        services.AddScoped<Maomi.MQ.Transaction.Postgres.PostgresTransactionDatabaseProvider>();
        services.AddScoped<IDatabaseProviderNamed>(s => s.GetRequiredService<Maomi.MQ.Transaction.Postgres.PostgresTransactionDatabaseProvider>());
        return services;
    }
}
