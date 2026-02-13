// <copyright file="Extensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ;

/// <summary>
/// MySQL transaction extension.
/// </summary>
public static class MaomiMqTransactionMySqlExtensions
{
    /// <summary>
    /// Registers MySQL transaction database provider.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection.</returns>
    public static IServiceCollection AddMaomiMQTransactionMySql(this IServiceCollection services)
    {
        services.AddScoped<Maomi.MQ.Transaction.Mysql.MySqlTransactionDatabaseProvider>();
        services.AddScoped<IDatabaseProviderNamed>(s => s.GetRequiredService<Maomi.MQ.Transaction.Mysql.MySqlTransactionDatabaseProvider>());
        return services;
    }
}
