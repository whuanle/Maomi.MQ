// <copyright file="ICircuitBreakerFacroty.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Polly;
using Polly.CircuitBreaker;

namespace Maomi.MQ.Retry;

/// <summary>
/// Default circuit breaker policy provider.
/// </summary>
public class DefaultCircuitBreakerFactory : ICircuitBreakerFactory
{
    /// <inheritdoc/>
    public AsyncCircuitBreakerPolicy Create(string queue)
    {
        var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(10));

        return circuitBreakerPolicy;
    }
}