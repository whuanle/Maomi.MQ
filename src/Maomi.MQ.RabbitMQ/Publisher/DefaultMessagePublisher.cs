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
    protected readonly IMessageSerializer _messageSerializer;
    protected readonly ConnectionPool _connectionPool;
    protected readonly IConnectionObject _connectionObject;
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
        _logger = loggerFactory.CreateLogger(DiagnosticName.Publisher);

        _consumerTypeProvider = new Lazy<IConsumerTypeProvider>(() => _serviceProvider.GetRequiredService<IConsumerTypeProvider>());
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
        _meterPushMessageBytes = _meter.CreateHistogram<long>("maomimq.consumer.message.received", "Byte", "The size of the received message", tags);
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

        _meter = publisher._meter;
        _meterPushMessageCount = publisher._meterPushMessageCount;
        _meterPushFaildMessageCount = publisher._meterPushFaildMessageCount;
        _meterPushMessageBytes = publisher._meterPushMessageBytes;
    }

    /// <inheritdoc />
    public virtual Task PublishAsync<TMessage>(TMessage message, Action<BasicProperties>? properties = null, CancellationToken cancellationToken = default)
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

        return CustomPublishAsync(consumerOptions.BindExchange ?? string.Empty, consumerOptions.RoutingKey ?? consumerOptions.Queue, message, basicProperties, cancellationToken);
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
    public virtual Task PublishAsync<TMessage>(TMessage message, BasicProperties? properties = default, CancellationToken cancellationToken = default)
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

        MessageHeader messageHeader = new MessageHeader
        {
            Id = _idGen.NextId().ToString(),
            Timestamp = DateTimeOffset.Now,
            AppId = _mqOptions.AppName,
            ContentEncoding = _messageSerializer.ContentEncoding,
            ContentType = _messageSerializer.ContentType,
            Type = typeof(TMessage).FullName!,
            UserId = properties.UserId ?? string.Empty,
            Properties = properties
        };

        InitializeMessageProperties<TMessage>(properties, ref messageHeader);

        OnStartEvent(ref messageHeader, exchange, reoutingKey, activity);

        byte[]? body = default;
        try
        {
            body = _messageSerializer.Serializer(message);
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
}

/// <summary>
/// <inheritdoc />
/// </summary>
public partial class DefaultMessagePublisher
{
    protected virtual void InitializeMessageProperties<TMessage>(BasicProperties properties, ref MessageHeader messageHeader)
    {
        properties.AppId = _mqOptions.AppName;
        properties.ContentEncoding = _messageSerializer.ContentEncoding;
        properties.ContentType = _messageSerializer.ContentType;
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
        activity.AddTag("ContentEncoding", messageHeader.ContentEncoding);
        activity.AddTag("ContentType", messageHeader.ContentType);
        activity.AddTag("Type", messageHeader.Type);
        activity.AddTag("UserId", messageHeader.UserId);
        activity.AddTag("Exchange", exchange);
        activity.AddTag("RoutingKey", reoutingKey);

        activity.Start();

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