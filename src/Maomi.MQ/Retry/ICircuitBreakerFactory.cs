// <copyright file="ICircuitBreakerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Polly.CircuitBreaker;

namespace Maomi.MQ.Retry;

/// <summary>
/// Circuit breaker policy provider.
/// 断路器策略提供器.
/// </summary>
public interface ICircuitBreakerFactory
{
    /// <summary>
    /// Create circuit breaker policy.<br />
    /// 创建断路器策略.
    /// </summary>
    /// <param name="queue"></param>
    /// <returns><see cref="AsyncCircuitBreakerPolicy"/>.</returns>
    AsyncCircuitBreakerPolicy Create(string queue);
}
