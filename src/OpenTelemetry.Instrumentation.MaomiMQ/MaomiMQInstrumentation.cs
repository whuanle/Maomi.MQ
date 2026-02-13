// <copyright file="MaomiMQInstrumentation.cs" company="Maomi">
// Copyright (c) Maomi. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Github link: https://github.com/whuanle/Maomi.MQ
// </copyright>

#pragma warning disable SA1600 // Elements should be documented

using Maomi.MQ.Diagnostics;
using OpenTelemetry.Instrumentation.MaomiMQ.Implementation;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenTelemetry.Instrumentation.MaomiMQ;

internal sealed class MaomiMQInstrumentation : IDisposable
{
    private readonly MaomiMQInstrumentationOptions options;
    private readonly object lockObject = new();
    private readonly List<IDisposable> listenerSubscriptions = new List<IDisposable>();

    private readonly IDisposable? allListenersSubscription;

    private bool disposed;

    public MaomiMQInstrumentation()
        : this(new MaomiMQInstrumentationOptions())
    {
    }

    public MaomiMQInstrumentation(MaomiMQInstrumentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        if (this.options.Enrich == null && !this.options.RecordException)
        {
            return;
        }

        this.allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new AllListenerObserver(this));
    }

    public void Dispose()
    {
        lock (this.lockObject)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.allListenersSubscription?.Dispose();

            foreach (var subscription in this.listenerSubscriptions)
            {
                subscription.Dispose();
            }

            this.listenerSubscriptions.Clear();
        }
    }

    private static bool IsMaomiMqListener(string listenerName)
    {
        return listenerName == DiagnosticName.Listener.Publisher
            || listenerName == DiagnosticName.Listener.Consumer;
    }

    private static bool HasExceptionEvent(Activity activity)
    {
        foreach (var activityEvent in activity.Events)
        {
            if (activityEvent.Name == "exception")
            {
                return true;
            }
        }

        return false;
    }

    private void HandleEvent(KeyValuePair<string, object?> value)
    {
        var activity = Activity.Current;
        if (activity == null || !activity.IsAllDataRequested)
        {
            return;
        }

        try
        {
            if (value.Value != null)
            {
                this.options.Enrich?.Invoke(activity, value.Key, value.Value);
            }
        }
        catch (Exception ex)
        {
            MaomiMQInstrumentationEventSource.Log.EnrichmentException(ex);
        }

        if (!this.options.RecordException || value.Value is not Exception exception)
        {
            return;
        }

        if (!HasExceptionEvent(activity))
        {
            activity.AddException(exception);
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    private void SubscribeListener(DiagnosticListener listener)
    {
        if (!IsMaomiMqListener(listener.Name))
        {
            return;
        }

        var subscription = listener.Subscribe(new EventObserver(this));

        lock (this.lockObject)
        {
            if (this.disposed)
            {
                subscription.Dispose();
                return;
            }

            this.listenerSubscriptions.Add(subscription);
        }
    }

    private sealed class AllListenerObserver : IObserver<DiagnosticListener>
    {
        private readonly MaomiMQInstrumentation instrumentation;

        public AllListenerObserver(MaomiMQInstrumentation instrumentation)
        {
            this.instrumentation = instrumentation;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            this.instrumentation.SubscribeListener(value);
        }
    }

    private sealed class EventObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly MaomiMQInstrumentation instrumentation;

        public EventObserver(MaomiMQInstrumentation instrumentation)
        {
            this.instrumentation = instrumentation;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            this.instrumentation.HandleEvent(value);
        }
    }
}
