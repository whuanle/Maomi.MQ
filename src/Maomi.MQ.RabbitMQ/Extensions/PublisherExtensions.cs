// <copyright file="PublisherExtensions.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ;

/// <summary>
/// extensions.
/// </summary>
public static class PublisherExtensions
{
    /// <summary>
    /// The publisher of an exclusive connection. <br />
    /// 独占连接对象的发布者.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static SinglePublisher CreateSingle(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var connection = messagePublisher.ConnectionPool.Get();
            var tran = new SinglePublisher(connection, publisher);
            return tran;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// The publisher of an exchange connection. <br />
    /// 创建用于发布到交换器的发布者.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static ExchangePublisher CreateExchange(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var p = new ExchangePublisher(publisher);
            return p;
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
    public static async Task<TransactionPublisher> TxSelectAsync(this IMessagePublisher messagePublisher)
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
    public static TransactionPublisher CreateTransaction(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var connection = messagePublisher.ConnectionPool.Get();
            var tran = new TransactionPublisher(connection, publisher);
            return tran;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }

    /// <summary>
    /// <inheritdoc cref="IChannel.ConfirmSelectAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static async Task<ConfirmPublisher> ConfirmSelectAsync(this IMessagePublisher messagePublisher)
    {
        var confirm = CreateConfirm(messagePublisher);
        await confirm.ConfirmSelectAsync();
        return confirm;
    }

    /// <summary>
    /// <inheritdoc cref="IChannel.ConfirmSelectAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="messagePublisher"></param>
    /// <returns><see cref="TransactionPublisher"/>.</returns>
    public static ConfirmPublisher CreateConfirm(this IMessagePublisher messagePublisher)
    {
        if (messagePublisher is DefaultMessagePublisher publisher)
        {
            var connection = messagePublisher.ConnectionPool.Get();
            var confirm = new ConfirmPublisher(connection, publisher);
            return confirm;
        }

        throw new InvalidCastException($"Unable to cast object of type '{messagePublisher.GetType().Name}' to type '{nameof(DefaultMessagePublisher)}'.");
    }
}