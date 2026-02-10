// <copyright file="MessageHeaderExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Maomi.MQ;

/// <summary>
/// Extensions for <see cref="IReadOnlyBasicProperties"/>.
/// </summary>
public static class MessageHeaderExtensions
{
    /// <summary>
    /// Get message header from <see cref="BasicDeliverEventArgs"/>.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns><see cref="MessageHeader"/>.</returns>
    public static MessageHeader GetMessageHeader(this BasicDeliverEventArgs eventArgs)
    {
        var properties = eventArgs.BasicProperties;

        var header = new MessageHeader
        {
            Id = properties.MessageId ?? string.Empty,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(properties.Timestamp.UnixTime),
            ContentType = properties.ContentType ?? string.Empty,
            Type = properties.Type ?? string.Empty,
            AppId = properties.AppId ?? string.Empty,
            Properties = properties,
            Exchange = eventArgs.Exchange,
            RoutingKey = eventArgs.RoutingKey
        };

        return header;
    }
    /// <summary>
    /// Get message header from <see cref="IReadOnlyBasicProperties"/>.
    /// </summary>
    /// <param name="properties"></param>
    /// <returns><see cref="MessageHeader"/>.</returns>
    public static MessageHeader GetMessageHeader(this IReadOnlyBasicProperties properties)
    {
        var header = new MessageHeader
        {
            Id = properties.MessageId ?? string.Empty,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(properties.Timestamp.UnixTime),
            ContentType = properties.ContentType ?? string.Empty,
            Type = properties.Type ?? string.Empty,
            AppId = properties.AppId ?? string.Empty,
            Properties = properties,
            Exchange = properties.Headers != null && properties.Headers.TryGetValue("exchange", out var exchange) ? exchange?.ToString() ?? string.Empty : string.Empty,
            RoutingKey = properties.Headers != null && properties.Headers.TryGetValue("routingKey", out var routingKey) ? routingKey?.ToString() ?? string.Empty : string.Empty
        };

        return header;
    }
}
