// <copyright file="ITypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Filters;

/// <summary>
/// Type filter.<br />
/// 类型过滤器.
/// </summary>
public interface ITypeFilter
{
    /// <summary>
    /// Filter type.<br />
    /// 过滤类型，处理类型.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="type"></param>
    void Filter(IServiceCollection services, Type type);

    /// <summary>
    /// Build.
    /// </summary>
    /// <param name="services"></param>
    /// <returns>ConsumerType collection.</returns>
    IEnumerable<ConsumerType> Build(IServiceCollection services);
}
