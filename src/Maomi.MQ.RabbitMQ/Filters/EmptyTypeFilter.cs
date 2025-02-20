// <copyright file="EmptyTypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Empty type filter.
/// </summary>
public class EmptyTypeFilter : ITypeFilter
{
    /// <inheritdoc />
    public IEnumerable<ConsumerType> Build(IServiceCollection services)
    {
        return Array.Empty<ConsumerType>();
    }

    /// <inheritdoc />
    public void Filter(IServiceCollection services, Type type)
    {
    }
}
