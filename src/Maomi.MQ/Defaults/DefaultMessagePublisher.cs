// <copyright file="MessagePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using IdGen;
using Maomi.MQ.Pool;
using RabbitMQ.Client;

namespace Maomi.MQ.Defaults;

/// <summary>
/// <inheritdoc />
/// </summary>
public class DefaultMessagePublisher : IMessagePublisher
{
    private readonly DefaultMqOptions _connectionOptions;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ConnectionPool _connectionPool;
    private readonly IIdGenerator<long> _idGen;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="connectionPool"></param>
    /// <param name="idGen"></param>
    public DefaultMessagePublisher(
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ConnectionPool connectionPool,
        IIdGenerator<long> idGen)
    {
        _connectionOptions = connectionOptions;
        _jsonSerializer = jsonSerializer;
        _connectionPool = connectionPool;
        _idGen = idGen;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null)
        where TEvent : class
    {
        var basicProperties = new BasicProperties()
        {
            AppId = _connectionOptions.ApplicationName,
            DeliveryMode = DeliveryModes.Persistent
        };

        if (properties != null)
        {
            properties.Invoke(basicProperties);
        }

        await PublishAsync(queue, message, basicProperties);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string queue, TEvent message, BasicProperties properties)
    {
        var eventBody = new EventBody<TEvent>
        {
            Id = _idGen.CreateId(),
            CreateTime = DateTimeOffset.Now,
            Body = message
        };

        await PublishAsync(queue, eventBody, properties);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        var connection = _connectionPool.Get();
        try
        {
            await connection.Channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                basicProperties: properties,
                body: _jsonSerializer.Serializer(message),
                mandatory: true);
        }
        finally
        {
            _connectionPool.Return(connection);
        }
    }
}
