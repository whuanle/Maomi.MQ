// <copyright file="PublisherDiagnostics.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Maomi.MQ.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Default publisher diagnostics implementation.
/// </summary>
public sealed class PublisherDiagnostics : IPublisherDiagnostics
{
    private const string MessagingSystem = "rabbitmq";

    private static readonly DiagnosticListener Listener = new DiagnosticListener(DiagnosticName.Listener.Publisher);
    private static readonly ActivitySource ActivitySource = new ActivitySource(DiagnosticName.ActivitySource.Publisher);

    private readonly Meter _meter;
    private readonly Counter<int> _messageCount;
    private readonly Counter<int> _messageFailCount;
    private readonly Histogram<long> _messageSize;

    private readonly bool _hasListeners;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublisherDiagnostics"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="mqOptions"></param>
    public PublisherDiagnostics(IServiceProvider serviceProvider, MqOptions mqOptions)
    {
        var meterFactory = serviceProvider.GetService<IMeterFactory>();
        _meter = meterFactory != null ? meterFactory.Create(DiagnosticName.Meter.Publisher) : SharedMeter.Publisher;

        var tags = new Dictionary<string, object?>
        {
            { nameof(MessageHeader.AppId), mqOptions.AppName }
        };

        _messageCount = _meter.CreateCounter<int>(
            DiagnosticName.Meter.PublisherMessageCount,
            unit: "{request}",
            description: "The total number of messages published.",
            tags);

        _messageFailCount = _meter.CreateCounter<int>(
            DiagnosticName.Meter.PublisherFaildMessageCount,
            unit: "{request}",
            description: "Total number of failed messages sent",
            tags);

        _messageSize = _meter.CreateHistogram<long>(
            DiagnosticName.Meter.PublisherMessageSent,
            unit: "Byte",
            description: "The size of the received message",
            tags);

        _hasListeners = Activity.Current != null || ActivitySource.HasListeners() || Listener.IsEnabled();
    }

    /// <inheritdoc />
    public Activity? Start(in MessageHeader messageHeader, string exchange, string routingKey)
    {
        var activity = ActivitySource.StartActivity(DiagnosticName.ActivitySource.Publisher, ActivityKind.Producer);

        if (!_hasListeners || activity == null)
        {
            return activity;
        }

        activity.SetTag("messaging.system", MessagingSystem);
        activity.SetTag("messaging.destination", exchange);
        activity.SetTag("messaging.destination_kind", "exchange");
        activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
        activity.SetTag("messaging.operation", "publish");
        activity.SetTag("messaging.message_id", messageHeader.Id);
        activity.SetTag("messaging.message_content_type", messageHeader.ContentType);
        activity.SetTag("messaging.message_type", messageHeader.Type);
        activity.SetTag("messaging.app_id", messageHeader.AppId);

        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", routingKey);
        tagList.Add("ContentType", messageHeader.ContentType);
        _messageCount.Add(1, tagList);

        Listener.Write(DiagnosticName.Event.PublisherStart, messageHeader);

        return activity;
    }

    /// <inheritdoc />
    public void Stop(in MessageHeader messageHeader, string exchange, string routingKey, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Stop();

        Listener.Write(DiagnosticName.Event.PublisherStop, messageHeader);
    }

    /// <inheritdoc />
    public void Exception(in MessageHeader messageHeader, string exchange, string routingKey, Exception exception, Activity? activity)
    {
        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", routingKey);
        tagList.Add("ContentType", messageHeader.ContentType);

        _messageFailCount.Add(1, tagList);

        if (!_hasListeners || activity == null)
        {
            return;
        }

#if NET9_0_OR_GREATER
        activity.AddException(exception, tagList);
#else
        DiagnosticsExtensions.AddException(activity, exception, tagList);
#endif
        activity.SetStatus(ActivityStatusCode.Error);

        Listener.Write(DiagnosticName.Event.PublisherExecption, exception);
    }

    /// <inheritdoc />
    public void RecordMessageSize(in MessageHeader messageHeader, string exchange, string routingKey, long size, Activity? activity)
    {
        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", routingKey);
        tagList.Add("ContentType", messageHeader.ContentType);

        _messageSize.Record(size, tagList);

        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.SetTag("messaging.message_payload_size_bytes", size);
    }
}
