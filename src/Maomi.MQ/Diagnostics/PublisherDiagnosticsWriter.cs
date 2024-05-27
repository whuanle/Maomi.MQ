using Polly;
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
public abstract class DiagnosticsWriter
{
    protected abstract DiagnosticListener Listener { get; }

    internal virtual Activity? WriteStarted(string activityName, DateTimeOffset startTimeUtc, ActivityTagsCollection tags)
    {
        if (!Listener.IsEnabled(activityName))
        {
            return null;
        }

        var activity = new Activity(activityName);
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
        var operationName = activity?.OperationName + ".Stop";
        if (activity != null && Listener.IsEnabled(activity?.OperationName))
        {
            activity.SetEndTime(endTimeUtc.UtcDateTime);
            Listener.StopActivity(activity, tags);
        }
    }

    public virtual void WriteException(Activity? activity, Exception exception)
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

public class PublisherDiagnosticsWriter : DiagnosticsWriter
{
    internal static readonly Lazy<System.Diagnostics.DiagnosticListener> DefaultListener =
            new(() => new System.Diagnostics.DiagnosticListener(DiagnosticName.MaomiMQ));

    protected override DiagnosticListener Listener => DefaultListener.Value;

}

public class ConsumerDiagnosticsWriter : DiagnosticsWriter
{
    internal static readonly Lazy<System.Diagnostics.DiagnosticListener> DefaultListener =
            new(() => new System.Diagnostics.DiagnosticListener(DiagnosticName.MaomiMQ));

    protected override DiagnosticListener Listener => DefaultListener.Value;

}
