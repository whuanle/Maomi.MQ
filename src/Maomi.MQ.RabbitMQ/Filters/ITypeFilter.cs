// <copyright file="ITypeFilter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Filters;

/// <summary>
/// This interface can be used to perform type detection and create the corresponding consumers or working modes.<br />
/// 可以通过此接口实现对类型的检测，创建对应的消费者或工作模式.
/// </summary>
public interface ITypeFilter
{
    /// <summary>
    /// Detect and categorize the type as a consumer.<br />
    /// 检测并将类型构建为消费者.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="type"></param>
    void Filter(IServiceCollection services, Type type);

    /// <summary>
    /// Obtain the list of consumer services.<br />
    /// 获取消费者服务列表.
    /// </summary>
    /// <param name="services"></param>
    /// <returns>ConsumerType collection.</returns>
    IEnumerable<ConsumerType> Build(IServiceCollection services);
}
