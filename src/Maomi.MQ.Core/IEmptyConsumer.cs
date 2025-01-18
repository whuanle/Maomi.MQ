// <copyright file="IEmptyConsumer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ;

/// <summary>
/// Empty consumer, only the definition is created, no consumption is performed.<br />
/// 空消费者，只创建定义，不执行消费.
/// </summary>
/// <typeparam name="TMessage">Event model.</typeparam>
public interface IEmptyConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
}