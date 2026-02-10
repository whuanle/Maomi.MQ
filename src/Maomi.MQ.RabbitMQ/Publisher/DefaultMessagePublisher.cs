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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc />
/// </summary>
public partial class DefaultMessagePublisher : IMessagePublisher, IChannelMessagePublisher
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly ConnectionPool _connectionPool;
    protected readonly IConnectionObject _connectionObject;
    protected readonly IIdProvider _idGen;
    protected readonly ILogger _logger;
    protected readonly IPublisherDiagnostics _publisherDiagnostics;

    protected readonly Lazy<IRoutingProvider> _routingProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="mqOptions"></param>
    /// <param name="connectionPool"></param>
    /// <param name="idGen"></param>
    /// <param name="loggerFactory"></param>
    public DefaultMessagePublisher(
        IServiceProvider serviceProvider,
        MqOptions mqOptions,
        ConnectionPool connectionPool,
        IIdProvider idGen,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _mqOptions = mqOptions;
        _connectionPool = connectionPool;
        _connectionObject = _connectionPool.Get();

        _idGen = idGen;
        _logger = loggerFactory.CreateLogger<DefaultMessagePublisher>();

        _publisherDiagnostics = serviceProvider.GetRequiredService<IPublisherDiagnostics>();

        _routingProvider = new Lazy<IRoutingProvider>(() => _serviceProvider.GetRequiredService<IRoutingProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    protected DefaultMessagePublisher(DefaultMessagePublisher publisher)
    {
        _serviceProvider = publisher._serviceProvider;
        _routingProvider = publisher._routingProvider;
        _mqOptions = publisher._mqOptions;
        _connectionPool = publisher._connectionPool;
        _connectionObject = publisher._connectionObject;
        _idGen = publisher._idGen;
        _logger = publisher._logger;
        _publisherDiagnostics = publisher._publisherDiagnostics;
    }

    /// <inheritdoc />
    public virtual Task AutoPublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var basicProperties = new BasicProperties()
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        if (properties != null)
        {
            properties.Invoke(basicProperties);
        }

        IQueueNameOptions? queueName = message.GetType().GetCustomAttribute<QueueNameAttribute>();
        if (queueName == null)
        {
            throw new InvalidOperationException($"The message type [{typeof(TMessage).FullName}] does not have the [{nameof(QueueNameAttribute)}] attribute.");
        }

        queueName = _routingProvider.Value.Get(queueName);

        return CustomPublishAsync(queueName.Exchange ?? string.Empty, queueName.RoutingKey, message, basicProperties, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, Action<BasicProperties> properties, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var basicProperties = new BasicProperties()
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        if (properties != null)
        {
            properties.Invoke(basicProperties);
        }

        return CustomPublishAsync(exchange, routingKey, message, basicProperties, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        return CustomPublishAsync(exchange, routingKey, message, properties, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        await PublishChannelAsync(_connectionObject.DefaultChannel, exchange, routingKey, message, properties, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task PublishChannelAsync<TMessage>(IChannel channel, string exchange, string routingKey, TMessage message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        IMessageSerializer? messageSerializer = default;
        foreach (var item in _mqOptions.MessageSerializers)
        {
            if (item.SerializerVerify(message))
            {
                messageSerializer = item;
                break;
            }
        }

        if (messageSerializer == null)
        {
            throw new InvalidOperationException($"No suitable message serializer found for message type [{typeof(TMessage).FullName}].");
        }

        MessageHeader messageHeader = new MessageHeader
        {
            Id = _idGen.NextId().ToString(),
            Timestamp = DateTimeOffset.Now,
            AppId = _mqOptions.AppName,
            ContentType = messageSerializer.ContentType,
            Type = typeof(TMessage).FullName!,
            Exchange = exchange,
            RoutingKey = routingKey,
            Properties = properties
        };

        InitializeMessageProperties<TMessage>(properties, ref messageHeader);

        using Activity? activity = _publisherDiagnostics.Start(messageHeader, exchange, routingKey);

        byte[]? body = default;
        try
        {
            body = messageSerializer.Serializer(message);
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body,
                mandatory: true,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The message with id [{Id}] failed to send, exchange: [{Exchange}], routingKey: [{RoutingKey}].", messageHeader.Id, exchange, routingKey);
            _publisherDiagnostics.Exception(messageHeader, exchange, routingKey, ex, activity);
            throw;
        }
        finally
        {
            _publisherDiagnostics.RecordMessageSize(messageHeader, exchange, routingKey, body?.Length ?? 0, activity);
            _publisherDiagnostics.Stop(messageHeader, exchange, routingKey, activity);
        }
    }

    /// <inheritdoc />
    public virtual async Task PublishChannelAsync(IChannel channel, string exchange, string routingKey, MessageHeader messageHeader, byte[] message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
        using Activity? activity = _publisherDiagnostics.Start(messageHeader, exchange, routingKey);

        try
        {
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: message,
                mandatory: true,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The message with id [{Id}] failed to send, exchange: [{Exchange}], routingKey: [{RoutingKey}].", messageHeader.Id, exchange, routingKey);
            _publisherDiagnostics.Exception(messageHeader, exchange, routingKey, ex, activity);
            throw;
        }
        finally
        {
            _publisherDiagnostics.RecordMessageSize(messageHeader, exchange, routingKey, message?.Length ?? 0, activity);
            _publisherDiagnostics.Stop(messageHeader, exchange, routingKey, activity);
        }
    }

    protected virtual void InitializeMessageProperties<TMessage>(BasicProperties properties, ref MessageHeader messageHeader)
    {
        IMessageSerializer? messageSerializer = default;
        foreach (var item in _mqOptions.MessageSerializers)
        {
            if (item.SerializerVerify<TMessage>())
            {
                messageSerializer = item;
                break;
            }
        }

        if (messageSerializer == null)
        {
            throw new InvalidOperationException($"No suitable message serializer found for message type [{typeof(TMessage).FullName}].");
        }

        properties.AppId = _mqOptions.AppName;
        properties.ContentType = messageSerializer.ContentType;
        properties.MessageId = messageHeader.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(messageHeader.Timestamp.ToUnixTimeMilliseconds());
        properties.Type = typeof(TMessage).FullName;

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
    }
}