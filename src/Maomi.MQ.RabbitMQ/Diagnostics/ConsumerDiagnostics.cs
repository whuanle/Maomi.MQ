// <copyright file="ConsumerDiagnostics.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maomi.MQ.Diagnostics;

/// <summary>
/// Default consumer diagnostics implementation.
/// </summary>
public sealed class ConsumerDiagnostics : IConsumerDiagnostics
{
    private const string MessagingSystem = "rabbitmq";

    private static readonly DiagnosticListener Listener = new DiagnosticListener(DiagnosticName.Listener.Consumer);
    private static readonly ActivitySource ActivitySource = new ActivitySource(DiagnosticName.ActivitySource.Consumer);
    private static readonly ActivitySource ExecuteSource = new ActivitySource(DiagnosticName.ActivitySource.Execute);
    private static readonly ActivitySource RetrySource = new ActivitySource(DiagnosticName.ActivitySource.Retry);
    private static readonly ActivitySource FallbackSource = new ActivitySource(DiagnosticName.ActivitySource.Fallback);

    private readonly Meter _meter;
    private readonly Counter<int> _pullMessageCount;
    private readonly Counter<int> _messageFailCount;
    private readonly Histogram<long> _messageSize;

    private readonly bool _hasListeners;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerDiagnostics"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public ConsumerDiagnostics(IServiceProvider serviceProvider)
    {
        var meterFactory = serviceProvider.GetService<IMeterFactory>();
        _meter = meterFactory != null ? meterFactory.Create(DiagnosticName.Meter.Consumer) : SharedMeter.Consumer;

        _pullMessageCount = _meter.CreateCounter<int>(
            "maomimq_consumer_message_pull_count",
            unit: "{request}",
            description: "The number of messages pushed or pulled by the server");

        _messageFailCount = _meter.CreateCounter<int>(
            "maomimq_consumer_message_faild_count",
            unit: "{request}",
            description: "The total number of retries for processing messages");

        _messageSize = _meter.CreateHistogram<long>(
            "maomimq_consumer_message_received",
            unit: "Byte",
            description: "The size of the received message");

        _hasListeners = Activity.Current != null || ActivitySource.HasListeners() || Listener.IsEnabled();
    }

    /// <inheritdoc />
    public Activity? StartConsume(in MessageHeader messageHeader, BasicDeliverEventArgs eventArgs, IConsumerOptions consumerOptions)
    {
        var activity = ActivitySource.StartActivity(DiagnosticName.ActivitySource.Consumer, ActivityKind.Consumer);

        TagList tagList = BuildTags(messageHeader, consumerOptions, eventArgs.Exchange, eventArgs.RoutingKey);
        _pullMessageCount.Add(1, tagList);
        _messageSize.Record(eventArgs.Body.Length, tagList);

        if (!_hasListeners || activity == null)
        {
            return activity;
        }

        activity.SetTag("messaging.system", MessagingSystem);
        activity.SetTag("messaging.destination", eventArgs.Exchange);
        activity.SetTag("messaging.destination_kind", "exchange");
        activity.SetTag("messaging.rabbitmq.routing_key", eventArgs.RoutingKey);
        activity.SetTag("messaging.operation", "process");
        activity.SetTag("messaging.message_id", messageHeader.Id);
        activity.SetTag("messaging.message_content_type", messageHeader.ContentType);
        activity.SetTag("messaging.message_type", messageHeader.Type);
        activity.SetTag("messaging.app_id", messageHeader.AppId);
        activity.SetTag("messaging.rabbitmq.queue", consumerOptions.Queue);

        Listener.Write(DiagnosticName.Event.ConsumerStart, messageHeader);

        return activity;
    }

    /// <inheritdoc />
    public void StopConsume(in MessageHeader messageHeader, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.Stop();
        Listener.Write(DiagnosticName.Event.ConsumerStop, messageHeader);
    }

    /// <inheritdoc />
    public void ExceptionConsume(in MessageHeader messageHeader, Exception exception, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.AddException(exception);
        activity.SetStatus(ActivityStatusCode.Error);
        Listener.Write(DiagnosticName.Event.ConsumerExecption, messageHeader);
    }

    /// <inheritdoc />
    public Activity? StartExecute(in MessageHeader messageHeader)
    {
        return ExecuteSource.StartActivity(DiagnosticName.ActivitySource.Execute, ActivityKind.Internal);
    }

    /// <inheritdoc />
    public void StopExecute(in MessageHeader messageHeader, Activity? activity)
    {
        activity?.Stop();
    }

    /// <inheritdoc />
    public void ExceptionExecute(in MessageHeader messageHeader, Exception exception, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.AddException(exception);
        activity.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public Activity? StartRetry(in MessageHeader messageHeader)
    {
        return RetrySource.StartActivity(DiagnosticName.ActivitySource.Retry, ActivityKind.Internal);
    }

    /// <inheritdoc />
    public void StopRetry(in MessageHeader messageHeader, Activity? activity)
    {
        activity?.Stop();
    }

    /// <inheritdoc />
    public void ExceptionRetry(in MessageHeader messageHeader, Exception exception, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.AddException(exception);
        activity.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public Activity? StartFallback(in MessageHeader messageHeader)
    {
        return FallbackSource.StartActivity(DiagnosticName.ActivitySource.Fallback, ActivityKind.Internal);
    }

    /// <inheritdoc />
    public void StopFallback(in MessageHeader messageHeader, ConsumerState fallbackState, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.SetTag("state", fallbackState);
        activity.Stop();
    }

    /// <inheritdoc />
    public void ExceptionFallback(in MessageHeader messageHeader, Exception exception, Activity? activity)
    {
        if (!_hasListeners || activity == null)
        {
            return;
        }

        activity.AddException(exception);
        activity.SetStatus(ActivityStatusCode.Error);
    }

    /// <inheritdoc />
    public void RecordFail(in MessageHeader messageHeader, IConsumerOptions consumerOptions)
    {
        TagList tagList = BuildTags(messageHeader, consumerOptions, consumerOptions.BindExchange, consumerOptions.RoutingKey);
        _messageFailCount.Add(1, tagList);
    }

    private static TagList BuildTags(in MessageHeader messageHeader, IConsumerOptions consumerOptions, string? exchange, string? routingKey)
    {
        TagList tagList = default;
        tagList.Add(nameof(MessageHeader.AppId), messageHeader.AppId);
        tagList.Add("Queue", consumerOptions.Queue);
        tagList.Add("Exchange", exchange);
        tagList.Add("RoutingKey", routingKey);
        tagList.Add("ContentType", messageHeader.ContentType);
        return tagList;
    }
}
