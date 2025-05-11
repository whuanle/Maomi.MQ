// <copyright file="FCommandAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// FastEndpoints message options.<br />
/// FastEndpoints 消息配置.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class FCommandAttribute : FastEndpointsCommandAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FCommandAttribute"/> class.
    /// </summary>
    /// <param name="queue"></param>
    public FCommandAttribute(string queue)
        : base(queue)
    {
    }
}