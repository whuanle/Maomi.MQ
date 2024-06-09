// <copyright file="DefaultMessagePublisher.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1401
#pragma warning disable SA1600
#pragma warning disable CS1591

using Maomi.MQ.Diagnostics;
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
    protected readonly DiagnosticsWriter _diagnosticsWriter = new DiagnosticsWriter();

    protected readonly MqOptions _mqOptions;
    protected readonly IJsonSerializer _jsonSerializer;
    protected readonly ConnectionPool _connectionPool;
    protected readonly IIdFactory _idGen;
    protected readonly ILogger<DefaultMessagePublisher> _logger;

    /// <inheritdoc />
    public ConnectionPool ConnectionPool => _connectionPool;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="mqOptions"></param>
    /// <param name="jsonSerializer"></param>
    /// <param name="connectionPool"></param>
    /// <param name="idGen"></param>
    /// <param name="logger"></param>
    public DefaultMessagePublisher(
        MqOptions mqOptions,
        IJsonSerializer jsonSerializer,
        ConnectionPool connectionPool,
        IIdFactory idGen,
        ILogger<DefaultMessagePublisher> logger)
    {
        _mqOptions = mqOptions;
        _jsonSerializer = jsonSerializer;
        _connectionPool = connectionPool;
        _idGen = idGen;
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<TEvent>(string queue, TEvent message, Action<IBasicProperties>? properties = null)
        where TEvent : class
    {
        var basicProperties = new BasicProperties()
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        if (properties != null)
        {
            properties.Invoke(basicProperties);
        }

        await PublishAsync(queue, message, basicProperties);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<TEvent>(string queue, TEvent message, BasicProperties properties)
    {
        var eventBody = new EventBody<TEvent>
        {
            Id = _idGen.NextId(),
            CreationTime = DateTimeOffset.Now,
            Publisher = _mqOptions.AppName,
            Body = message,
            Queue = queue
        };

        await PublishAsync(queue, eventBody, properties);
    }

    /// <inheritdoc />
    public virtual async Task PublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        var activityTags = message.GetTags();
        using Activity? activity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Publisher, DateTimeOffset.Now, activityTags);

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
        properties.Headers?.TryAdd(DiagnosticName.Event.Id, message.Id);
        properties.Headers?.TryAdd(DiagnosticName.Event.Publisher, _mqOptions.AppName);

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
            _diagnosticsWriter.WriteException(activity, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _connectionPool.Return(connection);
            _diagnosticsWriter.WriteStopped(activity, DateTimeOffset.Now, activityTags);
        }
    }
}
