// <copyright file="DefaultMqOptions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ.Defaults;

/// <summary>
/// Default options.
/// </summary>
public class DefaultMqOptions : MqOptions
{
    /// <summary>
    /// RabbitMQ connection factory.
    /// </summary>
    public ConnectionFactory ConnectionFactory { get; init; } = null!;
}
