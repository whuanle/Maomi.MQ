// <copyright file="MessageIdConverter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Globalization;

namespace Maomi.MQ.Transaction.Default;

internal static class MessageIdConverter
{
    public static long ParseRequired(string? messageId, string source)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new InvalidOperationException($"MessageId is required from {source}.");
        }

        if (!long.TryParse(messageId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOperationException($"MessageId '{messageId}' from {source} is invalid. Transaction module requires a 64-bit integer id.");
        }

        return value;
    }

    public static string ToText(long messageId)
    {
        return messageId.ToString(CultureInfo.InvariantCulture);
    }
}
