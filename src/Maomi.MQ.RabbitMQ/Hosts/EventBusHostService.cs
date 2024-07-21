// <copyright file="EventBusHostService.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Default;
using Maomi.MQ.Pool;
using Microsoft.Extensions.Logging;

namespace Maomi.MQ.Hosts;

/// <summary>
/// Base consumer service.Initialize the queue and build the consumer application.<br />
/// 初始化队列和构建消费者程序.
/// </summary>
public class EventBusHostService : ConsumerBaseHostService
{
    public EventBusHostService(IServiceProvider serviceProvider, ServiceFactory serviceFactory, ConnectionPool connectionPool, ILogger<ConsumerBaseHostService> logger, IReadOnlyList<ConsumerType> consumerTypes)
        : base(serviceProvider, serviceFactory, connectionPool, logger, consumerTypes)
    {
    }
}
