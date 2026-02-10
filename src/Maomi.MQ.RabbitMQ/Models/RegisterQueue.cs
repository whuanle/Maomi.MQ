// <copyright file="RegisterQueue.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Models;

/// <summary>
/// Information about the registration queue.<br />
/// 注册队列的信息.
/// </summary>
public class RegisterQueue : Tuple<bool, IConsumerOptions>
{
    /// <summary>
    /// Whether to register the queue.<br />
    /// 是否注册该队列.
    /// </summary>
    public bool IsRegister { get; init; }

    /// <summary>
    /// The consumption configuration of the queue.<br />
    /// 队列的消费配置.
    /// </summary>
    public IConsumerOptions Options { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterQueue"/> class.
    /// </summary>
    /// <param name="isRegister"></param>
    /// <param name="options"></param>
    public RegisterQueue(bool isRegister, IConsumerOptions options)
        : base(isRegister, options)
    {
        IsRegister = isRegister;
        Options = options;
    }
}
