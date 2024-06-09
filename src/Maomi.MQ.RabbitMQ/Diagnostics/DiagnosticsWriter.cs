// <copyright file="DiagnosticsWriter.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591

using System.Diagnostics;

namespace Maomi.MQ.Diagnostics;

/*
 Activity 不能设置 ActivityKind，需要通过 MaomiMQDiagnosticListener 拦截处理，请阅读 OpenTelemetry.Instrumentation.MaomiMQ 项目。
 */

// 1,DiagnosticsWriter 改造成 publisher 和 consumer 两个；
// 2，改造DiagnosticsName
// 3，改造 publisher 和 consumer

/// <summary>
/// Activity writer.
/// </summary>
public class DiagnosticsWriter
{
    internal static readonly Lazy<DiagnosticListener> DefaultListener =
            new(() => new DiagnosticListener(DiagnosticName.MaomiMQ));

    protected virtual DiagnosticListener Listener => DefaultListener.Value;

    internal virtual Activity? WriteStarted(string activityName, DateTimeOffset startTimeUtc, ActivityTagsCollection tags)
    {
        if (!Listener.IsEnabled(activityName))
        {
            return null;
        }

        var activity = new Activity(activityName);
        if (activity == null)
        {
            return default;
        }

        activity.SetStartTime(startTimeUtc.UtcDateTime);

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                activity.AddTag(tag.Key, tag.Value);
            }
        }

        Listener.StartActivity(activity, tags);
        return activity;
    }

    internal virtual void WriteStopped(Activity? activity, DateTimeOffset endTimeUtc, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (activity != null && Listener.IsEnabled(activity.OperationName))
        {
            activity.SetEndTime(endTimeUtc.UtcDateTime);
            Listener.StopActivity(activity, tags);
        }
    }

    internal virtual void WriteStopped(Activity? activity, DateTimeOffset endTimeUtc, ActivityTagsCollection tags)
    {
        if (activity != null && Listener.IsEnabled(activity.OperationName))
        {
            activity.SetEndTime(endTimeUtc.UtcDateTime);
            Listener.StopActivity(activity, tags);
        }
    }

    internal virtual void WriteException(Activity? activity, Exception exception)
    {
        var operationName = activity?.OperationName + ".Exception";
        if (activity != null && Listener.IsEnabled(activity.OperationName))
        {
            Listener.Write(operationName, exception);
        }
    }

    internal virtual void WriteEvent(Activity? activity, string eventName, ActivityTagsCollection? tags = null)
    {
        if (tags == null)
        {
            tags = new();
        }

        activity?.AddEvent(new ActivityEvent(eventName, DateTimeOffset.Now, tags));
    }

    internal virtual void WriteEvent(Activity? activity, string eventName, string key, object? value)
    {
        ActivityTagsCollection tags = new() { { key, value } };
        activity?.AddEvent(new ActivityEvent(eventName, DateTimeOffset.Now, tags));
    }
}
