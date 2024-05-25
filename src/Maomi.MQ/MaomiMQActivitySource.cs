using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;

namespace Maomi.MQ;

/// <summary>
/// ActivitySource.
/// </summary>
public static class MaomiMQActivitySource
{
    private static readonly string AssemblyVersion = typeof(MaomiMQActivitySource).Assembly.GetName()?.Version?.ToString() ?? "1.0.0.0";

    private static readonly ActivitySource PublisherSource = new ActivitySource(PublisherSourceName, AssemblyVersion);
    private static readonly ActivitySource SubscriberSource = new ActivitySource(SubscriberSourceName, AssemblyVersion);

    /// <summary>
    /// Maomi.MQ.Publisher.
    /// </summary>
    public const string PublisherSourceName = "Maomi.MQ.Publisher";

    /// <summary>
    /// Maomi.MQ.Subscriber.
    /// </summary>
    public const string SubscriberSourceName = "Maomi.MQ.Subscriber";

    /// <summary>
    /// Has listener.
    /// </summary>
    internal static bool PublisherHasListeners => PublisherSource.HasListeners();

    /// <summary>
    /// Has listener.
    /// </summary>
    internal static bool SubscriberHasListeners => SubscriberSource.HasListeners();

    internal static ActivityContext GetContext()
    {
        if (Activity.Current != null)
        {
            var activityContext = Activity.Current.Context;
            return activityContext;
        }

        return default;
    }

    internal static Activity? BuildActivity(string activityName, ActivityKind activityKind, ActivityContext activityContext = default)
    {
        if (PublisherHasListeners)
        {
            if (activityContext == default)
            {
                return PublisherSource.StartActivity(activityName, activityKind);
            }
            else
            {
                return PublisherSource.StartActivity(activityName, activityKind, activityContext);
            }
        }

        return default;
    }

    internal static bool TryGetExistingContext<T>(T props, out ActivityContext context)
        where T : IReadOnlyBasicProperties
    {
        if (props.Headers == null)
        {
            context = default;
            return false;
        }

        bool hasHeaders = false;
        foreach (string header in DistributedContextPropagator.Current.Fields)
        {
            if (props.Headers.ContainsKey(header))
            {
                hasHeaders = true;
                break;
            }
        }

        if (hasHeaders)
        {
            DistributedContextPropagator.Current.ExtractTraceIdAndState(props.Headers, ExtractTraceIdAndState,
                out string traceParent, out string traceState);
            return ActivityContext.TryParse(traceParent, traceState, out context);
        }

        context = default;
        return false;
    }

    private static void ExtractTraceIdAndState(object props, string name, out string value,
    out IEnumerable<string> values)
    {
        if (props is Dictionary<string, object> headers && headers.TryGetValue(name, out object propsVal) &&
            propsVal is byte[] bytes)
        {
            value = Encoding.UTF8.GetString(bytes);
            values = default;
        }
        else
        {
            value = default;
            values = default;
        }
    }
}
