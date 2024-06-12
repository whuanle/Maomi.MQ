// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Maomi.MQ.Diagnostics;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;
using System.Reflection;

namespace OpenTelemetry.Instrumentation.MaomiMQ.Implementation;

/// <summary>
/// 监听 Maomi.MQ 中的 Activity，并创建对应的 EventSource.
/// </summary>
internal sealed class MaomiMQDiagnosticListener : ListenerHandler
{
    /// <summary>
    /// OpenTelemetry.Instrumentation.MaomiMQ assembly.
    /// </summary>
    internal static readonly Assembly Assembly = typeof(MaomiMQDiagnosticListener).Assembly;

    /// <summary>
    /// OpenTelemetry.Instrumentation.MaomiMQ assembly version.
    /// </summary>
    internal static readonly AssemblyName AssemblyName = Assembly.GetName();

    /// <summary>
    /// <see cref="DiagnosticName.MaomiMQ"/>.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(DiagnosticName.MaomiMQ, Assembly.GetPackageVersion());

    private readonly MaomiMQInstrumentationOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaomiMQDiagnosticListener"/> class.
    /// </summary>
    /// <param name="sourceName"></param>
    /// <param name="options"></param>
    public MaomiMQDiagnosticListener(string sourceName, MaomiMQInstrumentationOptions options)
        : base(sourceName)
    {
        Guard.ThrowIfNull(options);
        this.options = options;
    }

    /// <inheritdoc />
    public override void OnEventWritten(string name, object? payload)
    {
        Activity? activity = Activity.Current;
        Guard.ThrowIfNull(activity);
        switch (name)
        {
            case DiagnosticName.Activity.Publisher + ".Start":
            case DiagnosticName.Activity.Consumer + ".Start":
            case DiagnosticName.Activity.Fallback + ".Start":
            case DiagnosticName.Activity.Execute + ".Start":
            case DiagnosticName.Activity.Retry + ".Start":
            case DiagnosticName.Activity.EventBus + ".Start":
                this.OnStartActivity(activity, payload);
                break;
            case DiagnosticName.Activity.Publisher + ".Stop":
            case DiagnosticName.Activity.Consumer + ".Stop":
            case DiagnosticName.Activity.Fallback + ".Stop":
            case DiagnosticName.Activity.Execute + ".Stop":
            case DiagnosticName.Activity.Retry + ".Stop":
            case DiagnosticName.Activity.EventBus + ".Stop":
                this.OnStopActivity(activity, payload);
                break;
            case DiagnosticName.Activity.Publisher + ".Execption":
            case DiagnosticName.Activity.Consumer + ".Execption":
            case DiagnosticName.Activity.Fallback + ".Execption":
            case DiagnosticName.Activity.Execute + ".Execption":
            case DiagnosticName.Activity.Retry + ".Execption":
            case DiagnosticName.Activity.EventBus + ".Execption":
                this.OnException(activity, payload);
                break;
        }
    }

    private static ActivityKind GetActivityKind(Activity activity)
    {
        return activity.OperationName switch
        {
            DiagnosticName.Activity.Publisher => ActivityKind.Producer,
            DiagnosticName.Activity.Consumer => ActivityKind.Consumer,
            DiagnosticName.Activity.Fallback => ActivityKind.Consumer,
            DiagnosticName.Activity.Execute => ActivityKind.Consumer,
            DiagnosticName.Activity.Retry => ActivityKind.Consumer,
            DiagnosticName.Activity.EventBus => ActivityKind.Consumer,
            _ => activity.Kind,
        };
    }

    private void OnStartActivity(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            ActivityInstrumentationHelper.SetActivitySourceProperty(activity, ActivitySource);
            ActivityInstrumentationHelper.SetKindProperty(activity, GetActivityKind(activity));
        }
    }

    private void OnStopActivity(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            try
            {
                if (payload != null)
                {
                    this.options.Enrich?.Invoke(activity, "OnStopActivity", payload);
                }
            }
            catch (Exception ex)
            {
                MaomiMQInstrumentationEventSource.Log.EnrichmentException(ex);
            }
        }
    }

    private void OnException(Activity activity, object? payload)
    {
        if (activity.IsAllDataRequested)
        {
            var exc = payload as Exception;
            if (exc == null)
            {
                MaomiMQInstrumentationEventSource.Log.NullPayload(nameof(MaomiMQDiagnosticListener), nameof(this.OnStopActivity));
                return;
            }

            if (this.options.RecordException)
            {
                activity.RecordException(exc);
            }

            activity.SetStatus(Status.Error.WithDescription(exc.Message));
        }
    }
}
