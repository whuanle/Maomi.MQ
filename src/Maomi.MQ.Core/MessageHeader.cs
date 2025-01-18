// <copyright file="MessageHeader.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// 头部注释.
/// </summary>
public class MessageHeader
{
    /// <summary>
    /// 全局雪花 id.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTimeOffset CreationTime { get; init; }

    /// <summary>
    /// 发布者.
    /// </summary>
    public string Publisher { get; init; } = null!;

    /// <summary>
    /// 不同 MQ 的属性信息不同，RabbitMQ 是 IBasicProperties.
    /// </summary>
    public object Properties { get; init; } = null!;
}
