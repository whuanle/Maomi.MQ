// <copyright file="MQConsumerTransactionOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Models;

/// <summary>
/// Consumer transaction options.
/// </summary>
public class MQConsumerTransactionOptions
{
    /// <summary>
    /// Table name.
    /// </summary>
    public string TableName { get; init; } = default!;
}
