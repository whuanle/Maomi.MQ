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
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Maomi.MQ;

/// <summary>
/// <inheritdoc />
/// </summary>
public class DefaultMessagePublisher : IMessagePublisher, IChannelMessagePublisher
{
    // todo: 除了链路追踪抽出来做全局单例
    protected readonly DiagnosticsWriter DiagnosticsWriter = new DiagnosticsWriter();
    protected readonly Meter _publisherMeter;
    protected readonly Counter<int> _publisherMessageCount;
    protected readonly Counter<int> _publisherMessageFaildCount;

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly IMessageSerializer _messageSerializer;
    protected readonly ConnectionPool _connectionPool;
    protected readonly ConnectionObject _connectionObject;
    protected readonly IIdFactory _idGen;
    protected readonly ILogger _logger;

    protected readonly Lazy<IConsumerTypeProvider> _consumerTypeProvider;
    protected readonly Lazy<IRoutingProvider> _routingProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="mqOptions"></param>
    /// <param name="messageSerializer"></param>
    /// <param name="connectionPool"></param>
    /// <param name="idGen"></param>
    /// <param name="loggerFactory"></param>
    public DefaultMessagePublisher(
        IServiceProvider serviceProvider,
        MqOptions mqOptions,
        IMessageSerializer messageSerializer,
        ConnectionPool connectionPool,
        IIdFactory idGen,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _mqOptions = mqOptions;
        _messageSerializer = messageSerializer;
        _connectionPool = connectionPool;
        _connectionObject = _connectionPool.Get();

        _idGen = idGen;
        _logger = loggerFactory.CreateLogger(DiagnosticName.PublisherName);

        _consumerTypeProvider = new Lazy<IConsumerTypeProvider>(() => _serviceProvider.GetRequiredService<IConsumerTypeProvider>());
        _routingProvider = new Lazy<IRoutingProvider>(() => _serviceProvider.GetRequiredService<IRoutingProvider>());

        var tags = new Dictionary<string, object?>()
        {
            { "AppName", _mqOptions.AppName }
        };

        _publisherMeter = new(DiagnosticName.PublisherName, Assembly.GetAssembly(typeof(DefaultMessagePublisher))!.GetName()!.Version!.ToString());
        _publisherMessageCount = _publisherMeter.CreateCounter<int>("maomimq.publisher.message.count", null, "Number of published messages", tags);
        _publisherMessageFaildCount = _publisherMeter.CreateCounter<int>("maomimq.publisher.message.faild.count", null, "Number of failed messages to publish", tags);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMessagePublisher"/> class.
    /// </summary>
    /// <param name="publisher"></param>
    protected DefaultMessagePublisher(DefaultMessagePublisher publisher)
    {
        _serviceProvider = publisher._serviceProvider;
        _consumerTypeProvider = publisher._consumerTypeProvider;
        _routingProvider = publisher._routingProvider;
        _mqOptions = publisher._mqOptions;
        _messageSerializer = publisher._messageSerializer;
        _connectionPool = publisher._connectionPool;
        _connectionObject = publisher._connectionObject;
        _idGen = publisher._idGen;
        _logger = publisher._logger;

        _publisherMeter = publisher._publisherMeter;
        _publisherMessageCount = publisher._publisherMessageCount;
        _publisherMessageFaildCount = publisher._publisherMessageFaildCount;
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null)
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

        var consumerOptions = _consumerTypeProvider.Value.First(x => x.Event == typeof(TMessage)).ConsumerOptions;
        consumerOptions = _routingProvider.Value.Get(consumerOptions);

        return CustomPublishAsync(consumerOptions.BindExchange ?? string.Empty, consumerOptions.RoutingKey ?? consumerOptions.Queue, message, basicProperties);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, Action<BasicProperties>? properties = null)
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

        return CustomPublishAsync(exchange, routingKey, message, basicProperties);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        return CustomPublishAsync(exchange, routingKey, message, properties);
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(TMessage message, BasicProperties? properties = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        var consumerOptions = _consumerTypeProvider.Value.First(x => x.Event == typeof(TMessage)).ConsumerOptions;
        consumerOptions = _routingProvider.Value.Get(consumerOptions);
        return CustomPublishAsync(consumerOptions.BindExchange ?? string.Empty, consumerOptions.RoutingKey ?? consumerOptions.Queue, message, properties);
    }

    /// <inheritdoc />
    public virtual async Task CustomPublishAsync<TMessage>(string exchange, string routingKey, TMessage message, BasicProperties? properties = default)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        await PublishChannelAsync(_connectionObject.DefaultChannel, exchange, routingKey, message, properties);
    }

    /// <inheritdoc />
    public virtual async Task PublishChannelAsync<TMessage>(IChannel channel, string exchange, string reoutingKey, TMessage message, BasicProperties properties)
    {
        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        MessageHeader messageHeader = new MessageHeader
        {
            Id = _idGen.NextId(),
            CreationTime = DateTimeOffset.Now,
            Publisher = _mqOptions.AppName
        };

        ActivityTagsCollection? activityTags = new ActivityTagsCollection();
        using Activity? activity = DiagnosticsWriter.WriteStarted(DiagnosticName.Activity.Publisher, DateTimeOffset.Now, activityTags);

        properties.AppId = _mqOptions.AppName;
        properties.ContentEncoding = _messageSerializer.ContentEncoding;
        properties.ContentType = _messageSerializer.ContentType;
        properties.MessageId = messageHeader.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(messageHeader.CreationTime.ToUnixTimeMilliseconds());
        properties.Type = typeof(TMessage).FullName; // todo: 后续优化，以便能够在不同语言框架中传递

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
        properties.Headers.TryAdd(DiagnosticName.MessageHeader.Id, messageHeader.Id);
        properties.Headers.TryAdd(DiagnosticName.MessageHeader.CreationTime, messageHeader.CreationTime);
        properties.Headers.TryAdd(DiagnosticName.MessageHeader.Publisher, messageHeader.Publisher);

        try
        {
            _publisherMessageCount.Add(1, new KeyValuePair<string, object?>("AppName", _mqOptions.AppName), new KeyValuePair<string, object?>("Queue", reoutingKey));

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: reoutingKey,
                basicProperties: properties,
                body: _messageSerializer.Serializer(message),
                mandatory: true);

            _logger.LogDebug("The message with id [{Id}] has been sent.", messageHeader.Id);

            DiagnosticsWriter.WriteStopped(activity, DateTimeOffset.Now, activityTags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The message with id [{Id}] failed to send.", messageHeader.Id);

            _publisherMessageFaildCount.Add(1, new KeyValuePair<string, object?>("AppName", _mqOptions.AppName), new KeyValuePair<string, object?>("Queue", reoutingKey));
            DiagnosticsWriter.WriteException(activity, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            DiagnosticsWriter.WriteStopped(activity, DateTimeOffset.Now, activityTags);

            throw;
        }
    }
}