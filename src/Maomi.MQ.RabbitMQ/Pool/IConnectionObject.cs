// <copyright file="IConnectionObject.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ.Pool;

/// <summary>
/// IConnection,IChannel pool.<br />
/// TCP 连接和通道.
/// </summary>
public interface IConnectionObject
{
    /// <summary>
    /// IConnection.
    /// </summary>
    IConnection Connection { get; }

    /// <summary>
    /// IChannel.
    /// </summary>
    IChannel DefaultChannel { get; }
}
