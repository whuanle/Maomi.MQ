// <copyright file="IWaitReadyFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Wait until the consumer service is ready to connect to RabbitMQ, create queues, generate configurations, and so on.<br />
/// 等待消费者服务准备就绪，就绪之前会连接 RabbitMQ、创建队列、生成配置等.
/// </summary>
public interface IWaitReadyFactory
{
    /// <summary>
    /// Add Task.
    /// </summary>
    /// <param name="task"></param>
    void AddTask(Task task);

    /// <summary>
    /// Wait for all tasks to complete.
    /// </summary>
    /// <returns><see cref="Task"/></returns>
    Task WaitReady();
}
