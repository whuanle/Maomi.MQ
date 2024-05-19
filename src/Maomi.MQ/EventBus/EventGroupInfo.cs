// <copyright file="EventGroupInfo.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.EventBus;

/// <summary>
/// Events are grouped, and queues of the same group are consumed in the same channel.<br />
/// 事件分组，相同分组的队列会被放到同一个通道中消费.
/// </summary>
public class EventGroupInfo
{
    /// <summary>
    /// Group name.<br />
    /// 分组名称.
    /// </summary>
    public string Group { get; init; } = null!;

    /// <summary>
    /// Event list.<br />
    /// 事件列表.
    /// </summary>
    public Dictionary<string, EventInfo> EventInfos { get; init; } = null!;

}
