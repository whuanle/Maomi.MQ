// <copyright file="EventOrderAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Attributes;

/// <summary>
/// Identifies the order of event handler.<br />
/// 标识事件执行器的顺序.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EventOrderAttribute : Attribute
{
    /// <summary>
    /// Order.<Br />
    /// 事件执行序号.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventOrderAttribute"/> class.
    /// </summary>
    /// <param name="order">Order.</param>
    public EventOrderAttribute(int order)
    {
        Order = order;
    }
}
