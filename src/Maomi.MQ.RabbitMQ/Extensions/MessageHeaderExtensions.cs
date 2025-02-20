// <copyright file="MessageHeaderExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Extensions for <see cref="IReadOnlyBasicProperties"/>.
/// </summary>
public static class MessageHeaderExtensions
{
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
            ContentEncoding = properties.ContentEncoding ?? string.Empty,
            Type = properties.Type ?? string.Empty,
            UserId = properties.UserId ?? string.Empty,
            AppId = properties.AppId ?? string.Empty,
            Properties = properties
        };
        return header;
    }
}
