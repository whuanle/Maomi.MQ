// <copyright file="ConsumerTypeProvider.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Consumer;

/// <summary>
/// Consumer type provider.
/// </summary>
public class ConsumerTypeProvider : List<ConsumerType>, IConsumerTypeProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ConsumerType> Consumers => this;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerTypeProvider"/> class.
    /// </summary>
    /// <param name="consumers"></param>
    public ConsumerTypeProvider(IReadOnlyList<ConsumerType> consumers)
    {
        AddRange(consumers);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerTypeProvider"/> class.
    /// </summary>
    public ConsumerTypeProvider()
    {
    }
}