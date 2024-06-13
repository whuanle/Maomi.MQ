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

namespace Maomi.MQ;

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

    // Copy.

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    protected DefaultMessagePublisher(DefaultMessagePublisher publisher)
    {
        _mqOptions = publisher._mqOptions;
        _jsonSerializer = publisher._jsonSerializer;
        _connectionPool = publisher.ConnectionPool;
        _idGen = publisher._idGen;
        _logger = publisher._logger;
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TEvent>(string queue, TEvent message, Action<BasicProperties>? properties = null)
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

        return PublishAsync(queue, message, basicProperties);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TEvent>(string queue, TEvent message, BasicProperties properties)
    {
        properties.DeliveryMode = DeliveryModes.Persistent;

        var eventBody = new EventBody<TEvent>
        {
            Id = _idGen.NextId(),
            CreationTime = DateTimeOffset.Now,
            Publisher = _mqOptions.AppName,
            Body = message,
            Queue = queue
        };

        return CustomPublishAsync(queue, eventBody, properties);
    }

    /// <inheritdoc />
    public virtual async Task CustomPublishAsync<TEvent>(string queue, EventBody<TEvent> message, BasicProperties properties)
    {
        var connection = _connectionPool.Get();
        try
        {
            await PublishAsync(connection.Channel, queue, message, properties);
        }
        finally
        {
            _connectionPool.Return(connection);
        }
    }

    /// <summary>
    /// Publish messagge.<br />
    /// 发布消息.
    /// </summary>
    /// <typeparam name="TEvent">Event model.<br />事件模型类.</typeparam>
    /// <param name="channel"></param>
    /// <param name="queue">Queue name.<br />队列名称.</param>
    /// <param name="message">Event object.<br />事件对象.</param>
    /// <param name="properties"><see href="https://rabbitmq.github.io/rabbitmq-dotnet-client/api/RabbitMQ.Client.IBasicProperties.html"/>.</param>
    /// <param name="exchange"></param>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual async Task PublishAsync<TEvent>(IChannel channel, string queue, EventBody<TEvent> message, BasicProperties properties, bool exchange = false)
    {
        var activityTags = message.GetTags();
        using Activity? activity = _diagnosticsWriter.WriteStarted(DiagnosticName.Activity.Publisher, DateTimeOffset.Now, activityTags);

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
        properties.Headers.TryAdd(DiagnosticName.Event.Id, message.Id);
        properties.Headers.TryAdd(DiagnosticName.Event.Publisher, _mqOptions.AppName);

        try
        {
            if (exchange)
            {
                await channel.BasicPublishAsync(
                    exchange: queue,
                    routingKey: string.Empty,
                    basicProperties: properties,
                    body: _jsonSerializer.Serializer(message),
                    mandatory: true);
            }
            else
            {
                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queue,
                    basicProperties: properties,
                    body: _jsonSerializer.Serializer(message),
                    mandatory: true);
            }

            _logger.LogDebug("The message with id [{Id}] has been sent.", message.Id);
            _diagnosticsWriter.WriteStopped(activity, DateTimeOffset.Now, activityTags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The message with id [{Id}] failed to send.", message.Id);
            _diagnosticsWriter.WriteException(activity, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _diagnosticsWriter.WriteStopped(activity, DateTimeOffset.Now, activityTags);
            throw;
        }
    }
}