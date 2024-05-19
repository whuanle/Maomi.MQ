// <copyright file="IdGenExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using IdGen;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Extensions;

/// <summary>
/// x.
/// </summary>
public static class IdGenExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IdGenerator"/> with the given <paramref name="generatorId"/> and <see cref="IdGeneratorOptions"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the singleton <see cref="IdGenerator"/> on.</param>
    /// <param name="generatorId">The generator-id to use for the singleton <see cref="IdGenerator"/>.</param>
    /// <param name="options">The <see cref="IdGeneratorOptions"/> for the singleton <see cref="IdGenerator"/>.</param>
    /// <returns>The given <see cref="IServiceCollection"/> with the registered singleton <see cref="IdGenerator"/> in it.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static IServiceCollection AddIdGen(this IServiceCollection services, int generatorId, Func<IdGeneratorOptions> options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        services.AddSingleton<IIdGenerator<long>>(new IdGenerator(generatorId, options()));
        services.AddSingleton<IdGenerator>(c => (IdGenerator)c.GetRequiredService<IIdGenerator<long>>());

        return services;
    }
}
