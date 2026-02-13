// <copyright file="IDatabaseProviderNamed.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Database;

/// <summary>
/// Named database provider metadata.
/// </summary>
public interface IDatabaseProviderNamed
{
    /// <summary>
    /// Gets provider name.
    /// </summary>
    string ProviderName { get; }
}
