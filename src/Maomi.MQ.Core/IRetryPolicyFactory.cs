// <copyright file="IRetryPolicyFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Polly.Retry;

namespace Maomi.MQ;

/// <summary>
/// Retry policy factory.<br />
/// 重试策略工厂.
/// </summary>
public interface IRetryPolicyFactory
{
    /// <summary>
    /// Create retry policy.<br />
    /// 创建策略.
    /// </summary>
    /// <param name="queue">Queue name.<br />队列名称.</param>
    /// <param name="id">Event id.</param>
    /// <returns><see cref="Task{AsyncRetryPolicy}"/>.</returns>
    Task<AsyncRetryPolicy> CreatePolicy(string queue, long id);
}
