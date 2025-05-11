// <copyright file="IMessageSerializerFactory.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

namespace Maomi.MQ.Default;

/// <summary>
/// Message serializer factory.
/// </summary>
public interface IMessageSerializerFactory : IMessageSerializer
{
    /// <summary>
    /// Get message serializer.
    /// </summary>
    /// <param name="type"></param>
    /// <returns><see cref="IMessageSerializer"/>.</returns>
    IMessageSerializer GetMessageSerializer(Type type);

    /// <summary>
    /// Get message deserializer.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="messageHeader"></param>
    /// <returns><see cref="IMessageSerializer"/>.</returns>
    IMessageSerializer GetMessageDeserializer(Type type, MessageHeader messageHeader);
}
