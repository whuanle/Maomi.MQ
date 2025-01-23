// <copyright file="PublisherExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// Extensions.
/// </summary>
public static class PublisherExtensions
{
    /// <summary>
    /// The publisher of an exclusive connection channel. <br />
    /// 独占连接通道的发布者.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <param name="createChannelOptions"><see cref="CreateChannelOptions"/>.</param>
    /// <returns><see cref="ISingleChannelPublisher"/>.</returns>
    public static ISingleChannelPublisher CreateSingle(this IMessagePublisher messagePublisher, CreateChannelOptions? createChannelOptions = default)
    {
        if (createChannelOptions == null)
        {
            createChannelOptions = new CreateChannelOptions(publisherConfirmationsEnabled: false, publisherConfirmationTrackingEnabled: false);
        }

        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            return new SingleChannelPublisher(publisher, createChannelOptions);
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// See <see cref="IChannel.TxSelectAsync(CancellationToken)"/>. <br />
    /// Asynchronously enable TX mode for this session.<br />
    /// 开启事务.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static async Task<ITransactionPublisher> TxSelectAsync(this IMessagePublisher messagePublisher)
    {
        var tran = CreateTransaction(messagePublisher);
        await tran.TxSelectAsync();
        return tran;
    }

    /// <summary>
    /// See <see cref="IChannel.TxSelectAsync(CancellationToken)"/>. <br />
    /// Asynchronously enable TX mode for this session.<br />
    /// 开启事务.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static ITransactionPublisher CreateTransaction(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var tran = new TransactionPublisher(publisher);
            return tran;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }
}