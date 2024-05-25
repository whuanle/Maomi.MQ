// <copyright file="DefaultMessagePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1204

using IdGen;
using Maomi.MQ.Pool;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Diagnostics;

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
    private readonly ILogger<DefaultMessagePublisher> _logger;

    /// <inheritdoc />
    public ConnectionPool ConnectionPool => _connectionPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="connectionOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="connectionPool"></param>
    /// <param name="idGen"></param>
    /// <param name="logger"></param>
    public DefaultMessagePublisher(
        DefaultMqOptions connectionOptions,
        IJsonSerializer jsonSerializer,
        ConnectionPool connectionPool,
        IIdGenerator<long> idGen,
        ILogger<DefaultMessagePublisher> logger)
    {
        _connectionOptions = connectionOptions;
        _jsonSerializer = jsonSerializer;
        _connectionPool = connectionPool;
        _idGen = idGen;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null)
        where TEvent : class
    {
        var basicProperties = new BasicProperties()
        {
            //AppId = _connectionOptions.ApplicationName,
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
            Body = message,
            Queue = queue
        };

        await PublishAsync(queue, eventBody, properties);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        var activityName = $"send {queue}";

        var activityContext = MaomiMQActivitySource.GetContext();
        using Activity? activity = MaomiMQActivitySource.BuildActivity($"{message.Id} publish", ActivityKind.Producer, activityContext);

        if (activity != null)
        {
            //Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), properties, InjectTraceContextIntoBasicProperties);
            AddMessagingTags(activity, message);
        }

        var connection = _connectionPool.Get();
        try
        {
            await connection.Channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                basicProperties: properties,
                body: _jsonSerializer.Serializer(message),
                mandatory: true);

            _logger.LogDebug("The message with id [{Id}] has been sent.", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The message with id [{Id}] failed to send.", message.Id);
            throw;
        }
        finally
        {
            _connectionPool.Return(connection);
        }
    }

    private void InjectTraceContextIntoBasicProperties(BasicProperties props, string key, string value)
    {
        try
        {
            if (props.Headers == null)
            {
                props.Headers = new Dictionary<string, object?>();
            }

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject trace context.");
        }
    }

    private static void AddMessagingTags<TEvent>(Activity activity, EventBody<TEvent> message)
    {
        activity?.SetTag("event.id", message.Id);
    }
}
