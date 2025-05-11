// <copyright file="RCommandAttribute.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.MediatR;

/// <summary>
/// MediatR message options.<br />
/// MediatR 消息配置.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RCommandAttribute : MediarCommandAttribute
{

    /// <summary>
    /// Initializes a new instance of the <see cref="RCommandAttribute"/> class.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    public RCommandAttribute(string queue)
        : base(queue)
    {
        ArgumentException.ThrowIfNullOrEmpty(queue, nameof(queue));
        Queue = queue;
    }
}
