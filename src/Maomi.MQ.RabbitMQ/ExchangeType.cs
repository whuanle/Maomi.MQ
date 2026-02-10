// <copyright file="ExchangeType.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using System.Text.Json.Serialization;

namespace Maomi.MQ;

/// <summary>
/// Rabbit exchange type, fanout, firect, topic, headers.
/// </summary>
public enum ExchangeType
{
    /// <summary>
    /// fanout.<br />
    /// </summary>
    [JsonPropertyName("fanout")]
    Fanout = 0,

    /// <summary>
    /// direct.<br />
    /// </summary>
    [JsonPropertyName("direct")]
    Direct,

    /// <summary>
    /// topic.<br />
    /// </summary>
    [JsonPropertyName("topic")]
    Topic,

    /// <summary>
    /// headers.
    /// </summary>
    [JsonPropertyName("headers")]
    Headers
}