// <copyright file="MaomiMQInstrumentationEventSource.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented

using System;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace OpenTelemetry.Instrumentation.MaomiMQ.Implementation;

/// <summary>
/// EventSource events emitted from the project.
/// </summary>
[EventSource(Name = "OpenTelemetry-Instrumentation-MaomiMQ")]
internal class MaomiMQInstrumentationEventSource : EventSource
{
    public static readonly MaomiMQInstrumentationEventSource Log = new();

    [Event(1, Message = "Payload is NULL in event '{1}' from handler '{0}', span will not be recorded.", Level = EventLevel.Warning)]
    public void NullPayload(string handlerName, string eventName)
    {
        this.WriteEvent(1, handlerName, eventName);
    }

    [Event(2, Message = "Request is filtered out.", Level = EventLevel.Verbose)]
    public void OperationIsFilteredOut(string eventName)
    {
        this.WriteEvent(2, eventName);
    }

    [NonEvent]
    public void EnrichmentException(Exception ex)
    {
        if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
        {
            this.EnrichmentException(ToInvariantString(ex));
        }
    }

    [Event(3, Message = "Enrich threw exception. Exception {0}.", Level = EventLevel.Error)]
    public void EnrichmentException(string exception)
    {
        this.WriteEvent(3, exception);
    }

    [NonEvent]
    public void UnknownErrorProcessingEvent(string handlerName, string eventName, Exception ex)
    {
        if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
        {
            this.UnknownErrorProcessingEvent(handlerName, eventName, ToInvariantString(ex));
        }
    }

    [Event(4, Message = "Unknown error processing event '{1}' from handler '{0}', Exception: {2}", Level = EventLevel.Error)]
    public void UnknownErrorProcessingEvent(string handlerName, string eventName, string ex)
    {
        this.WriteEvent(4, handlerName, eventName, ex);
    }

    private static string ToInvariantString(Exception exception)
    {
        var originalUICulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
        }
    }
}
