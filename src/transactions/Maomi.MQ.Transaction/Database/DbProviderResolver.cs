// <copyright file="DbProviderResolver.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Transaction.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Resolves <see cref="IDatabaseProvider"/> by provider name.
/// </summary>
public static class DbProviderResolver
{
    /// <summary>
    /// Resolves registered provider implementation.
    /// </summary>
    /// <param name="serviceProvider">Service provider.</param>
    /// <returns><see cref="IDatabaseProvider"/>.</returns>
    public static IDatabaseProvider Resolve(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IMQTransactionOptions>();
        var providerName = options.ProviderName;

        var providers = serviceProvider.GetServices<IDatabaseProviderNamed>();
        foreach (var provider in providers)
        {
            if (providerName.Equals(provider.ProviderName, StringComparison.OrdinalIgnoreCase)
                && provider is IDatabaseProvider databaseProvider)
            {
                return databaseProvider;
            }
        }

        throw new InvalidOperationException($"No transaction database provider found for providerName '{providerName}'.");
    }
}
