// <copyright file="DefaultCircuitBreakerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Polly;
using Polly.CircuitBreaker;

namespace Maomi.MQ.Default;

/*
    Not used for the time being.
    暂时不使用.
 */

/// <summary>
/// Default circuit breaker policy provider.
/// 熔断策略.
/// </summary>
public class DefaultCircuitBreakerFactory : ICircuitBreakerFactory
{
    private const int CircuitCount = 5;
    private const int CircuitSeconds = 10;

    /// <inheritdoc/>
    public AsyncCircuitBreakerPolicy Create(string queue)
    {
        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: CircuitCount,
                durationOfBreak: TimeSpan.FromSeconds(CircuitSeconds));

        return circuitBreakerPolicy;
    }
}