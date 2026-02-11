// <copyright file="TransactionMessageSerializer.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Transaction.Default;

/// <summary>
/// Selects serializer from configured serializers.
/// </summary>
public sealed class TransactionMessageSerializer : ITransactionMessageSerializer
{
    private readonly MqOptions _mqOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionMessageSerializer"/> class.
    /// </summary>
    /// <param name="mqOptions">MQ options.</param>
    public TransactionMessageSerializer(MqOptions mqOptions)
    {
        _mqOptions = mqOptions;
    }

    /// <inheritdoc/>
    public IMessageSerializer GetSerializer<TMessage>(TMessage message)
    {
        var messageType = message?.GetType() ?? typeof(TMessage);

        foreach (var serializer in _mqOptions.MessageSerializers)
        {
            if (serializer.SerializerVerify(message))
            {
                return serializer;
            }
        }

        throw new InvalidOperationException($"No suitable message serializer found for message type [{messageType.FullName}].");
    }
}
