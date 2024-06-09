// <copyright file="ConsumerType.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Default;
using Maomi.MQ.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// Consumer info.
/// </summary>
public class ConsumerType
{
    /// <summary>
    /// Queue.
    /// </summary>
    public string Queue { get; init; } = null!;

    /// <summary>
    /// <see cref="IConsumer{TEvent}"/>.
    /// </summary>
    public Type Consumer { get; init; } = null!;

    /// <summary>
    /// Event model.
    /// </summary>
    public Type Event { get; init; } = null!;
}
