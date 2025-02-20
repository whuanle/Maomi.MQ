// <copyright file="DefaultBreakdown.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client.Events;

namespace Maomi.MQ.Default;

/// <summary>
/// Fault processor, which calls the relevant interface when the program fails.<br />
/// 故障处理器，当程序出现故障时会调用相关接口.
/// </summary>
public class DefaultBreakdown : IBreakdown
{
    /// <inheritdoc />
    public Task BasicReturnAsync(object sender, BasicReturnEventArgs @event)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotFoundConsumerAsync(string queue, Type messageType, Type consumerType)
    {
        return Task.CompletedTask;
    }
}