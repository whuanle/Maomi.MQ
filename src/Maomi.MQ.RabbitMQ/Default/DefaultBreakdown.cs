// <copyright file="DefaultBreakdown.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace Maomi.MQ.Default;

/// <summary>
/// Fault processor, which calls the relevant interface when the program fails.<br />
/// 故障处理器，当程序出现故障时会调用相关接口.
/// </summary>
public class DefaultBreakdown : IBreakdown
{
    private readonly ILogger<DefaultBreakdown> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBreakdown"/> class.
    /// </summary>
    /// <param name="logger"></param>
    public DefaultBreakdown(ILogger<DefaultBreakdown> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task BasicReturnAsync(object sender, BasicReturnEventArgs @event)
    {
        _logger.LogError("Message returned,reply code [{ReplyCode}],reply text [{ReplyText}],exchange [{Exchange}],routing key [{RoutingKey}]", @event.ReplyCode, @event.ReplyText, @event.Exchange, @event.RoutingKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task NotFoundConsumerAsync(string queue, Type messageType, Type consumerType)
    {
        _logger.LogError("Not found consumer for queue [{Queue}], message type [{MessageType}], consumer type [{ConsumerType}]", queue, messageType.FullName, consumerType.FullName);
        return Task.CompletedTask;
    }
}