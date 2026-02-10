// <copyright file="IConsumerTypeProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Consumer;

namespace Maomi.MQ;

/// <summary>
/// Consumer type provider.
/// </summary>
public interface IConsumerTypeProvider : IReadOnlyList<ConsumerType>
{
    /// <summary>
    /// Consumers.
    /// </summary>
    IReadOnlyList<ConsumerType> Consumers { get; }
}
