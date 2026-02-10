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
public partial class DefaultMessagePublisher : IMessagePublisher, IChannelMessagePublisher
{
    protected static readonly DiagnosticListener _diagnosticListener = new DiagnosticListener(DiagnosticName.Listener.Publisher);
    protected static readonly ActivitySource _activitySource = new ActivitySource(DiagnosticName.ActivitySource.Publisher);

    protected readonly Meter _meter;
    protected readonly Counter<int> _meterPushMessageCount;
    protected readonly Counter<int> _meterPushFaildMessageCount;
    protected readonly Histogram<long> _meterPushMessageBytes;

    protected readonly IServiceProvider _serviceProvider;
    protected readonly MqOptions _mqOptions;
    protected readonly ConnectionPool _connectionPool;
    protected readonly IConnectionObject _connectionObject;
    protected readonly IIdProvider _idGen;
    protected readonly ILogger _logger;

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

        _routingProvider = new Lazy<IRoutingProvider>(() => _serviceProvider.GetRequiredService<IRoutingProvider>());

        var tags = new Dictionary<string, object?>()
        {
            { nameof(MessageHeader.AppId), _mqOptions.AppName }
        };

        var meterFactory = serviceProvider.GetService<IMeterFactory>();

        _meter = meterFactory != null ? meterFactory.Create(DiagnosticName.Meter.Publisher) : SharedMeter.Publisher;
        _meterPushMessageCount = _meter.CreateCounter<int>(
            DiagnosticName.Meter.PublisherMessageCount,
            unit: "{request}",
            description: "The total number of messages published.",
            tags);
        _meterPushFaildMessageCount = _meter.CreateCounter<int>(
            DiagnosticName.Meter.PublisherFaildMessageCount,
            unit: "{request}",
            "Total number of failed messages sent",
            tags);
        _meterPushMessageBytes = _meter.CreateHistogram<long>(DiagnosticName.Meter.PublisherMessageSent, "Byte", "The size of the received message", tags);
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

        _meter = publisher._meter;
        _meterPushMessageCount = publisher._meterPushMessageCount;
        _meterPushFaildMessageCount = publisher._meterPushFaildMessageCount;
        _meterPushMessageBytes = publisher._meterPushMessageBytes;
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
    public virtual async Task PublishChannelAsync<TMessage>(IChannel channel, string exchange, string reoutingKey, TMessage message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        using Activity? activity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Publisher, ActivityKind.Producer);

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
            RoutingKey = reoutingKey,
            Properties = properties
        };

        InitializeMessageProperties<TMessage>(properties, ref messageHeader);

        OnStartEvent(ref messageHeader, exchange, reoutingKey, activity);

        byte[]? body = default;
        try
        {
            body = messageSerializer.Serializer(message);
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: reoutingKey,
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
            _logger.LogWarning(ex, "The message with id [{Id}] failed to send, exchange: [{Exchange}], reoutingKey: [{reoutingKey}].", messageHeader.Id, exchange, reoutingKey);
            OnExecptionEvent(ref messageHeader, exchange, reoutingKey, ex, activity);
            throw;
        }
        finally
        {
            _meterPushMessageBytes.Record(body?.Length ?? 0);
            OnStopEvent(ref messageHeader, exchange, reoutingKey, activity);
        }
    }

    /// <inheritdoc />
    public virtual async Task PublishChannelAsync(IChannel channel, string exchange, string reoutingKey, MessageHeader messageHeader, byte[] message, BasicProperties properties, CancellationToken cancellationToken = default)
    {
        using Activity? activity = _activitySource.StartActivity(DiagnosticName.ActivitySource.Publisher, ActivityKind.Producer);

        if (properties == null)
        {
            properties = new BasicProperties()
            {
                DeliveryMode = DeliveryModes.Persistent
            };
        }

        properties.Headers = properties.Headers ?? new Dictionary<string, object?>();
        OnStartEvent(ref messageHeader, exchange, reoutingKey, activity);

        try
        {
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: reoutingKey,
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
            _logger.LogWarning(ex, "The message with id [{Id}] failed to send, exchange: [{Exchange}], reoutingKey: [{reoutingKey}].", messageHeader.Id, exchange, reoutingKey);
            OnExecptionEvent(ref messageHeader, exchange, reoutingKey, ex, activity);
            throw;
        }
        finally
        {
            _meterPushMessageBytes.Record(message?.Length ?? 0);
            OnStopEvent(ref messageHeader, exchange, reoutingKey, activity);
        }
    }
}

/// <summary>
/// <inheritdoc />
/// </summary>
public partial class DefaultMessagePublisher
{
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

    protected bool IsEnabledListener()
    {
        // check if there is a parent Activity or if someone listens to "<Maomi.MQ.Publisher>" ActivitySource or "MaomiMQPublisherHandlerDiagnosticListener" DiagnosticListener.
        return Activity.Current != null ||
               _activitySource.HasListeners() ||
               _diagnosticListener.IsEnabled();
    }

    protected virtual void OnStartEvent(ref MessageHeader messageHeader, string exchange, string reoutingKey, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.AddTag("Id", messageHeader.Id);
        activity.AddTag("Timestamp", messageHeader.Timestamp);
        activity.AddTag("AppId", messageHeader.AppId);
        activity.AddTag("ContentType", messageHeader.ContentType);
        activity.AddTag("Type", messageHeader.Type);
        activity.AddTag("Exchange", exchange);
        activity.AddTag("RoutingKey", reoutingKey);

        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", reoutingKey);

        _meterPushMessageCount.Add(1, tagList);
        _diagnosticListener.Write(DiagnosticName.Event.PublisherStart, messageHeader);
    }

    protected virtual void OnStopEvent(ref MessageHeader messageHeader, string exchange, string reoutingKey, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Stop();

        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", reoutingKey);

        _diagnosticListener.Write(DiagnosticName.Event.PublisherStop, messageHeader);
    }

    protected virtual void OnExecptionEvent(ref MessageHeader messageHeader, string exchange, string reoutingKey, Exception exception, Activity? activity)
    {
        if (activity == null || !IsEnabledListener())
        {
            return;
        }

        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", reoutingKey);

#if NET9_0_OR_GREATER
        activity.AddException(exception, tagList);
#else
        DiagnosticsExtensions.AddException(activity, exception, tagList);
#endif
        activity.SetStatus(ActivityStatusCode.Error);
        _meterPushFaildMessageCount.Add(1, tagList);

        _diagnosticListener.Write(DiagnosticName.Event.PublisherExecption, exception);
    }
}