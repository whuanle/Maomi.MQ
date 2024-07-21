// <copyright file="ExchangePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable CS1591
#pragma warning disable SA1401
#pragma warning disable SA1600

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc cref="IChannel.WaitForConfirmsAsync(CancellationToken)"/>
/// </summary>
public class ExchangePublisher : DefaultMessagePublisher, IMessagePublisher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangePublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    internal ExchangePublisher(DefaultMessagePublisher publisher)
        : base(publisher)
    {
    }

    /// <inheritdoc cref="IMessagePublisher.CustomPublishAsync{TEvent}(string, EventBody{TEvent}, BasicProperties)"/>
    public override async Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        var connection = _connectionPool.Get();
        await PublishAsync(connection.DefaultChannel, queue, message, properties, true);
    }

    /// <inheritdoc />
    protected override Task PublishAsync<TEvent>(IChannel channel, string queue, EventBody<TEvent> message, BasicProperties properties, bool exchange = false)
    {
        return base.PublishAsync(channel, queue, message, properties, true);
    }
}
