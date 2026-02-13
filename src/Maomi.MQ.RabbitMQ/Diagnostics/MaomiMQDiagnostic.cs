// <copyright file="MaomiMQDiagnostic.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// ActivitySource names.
/// </summary>
public static class MaomiMQDiagnostic
{
    /// <summary>
    /// ActivitySource names.
    /// </summary>
    public static readonly IReadOnlyList<string> Sources = new List<string>
    {
        RabbitMQActivitySource.PublisherSourceName,
        RabbitMQActivitySource.SubscriberSourceName
    };
}
